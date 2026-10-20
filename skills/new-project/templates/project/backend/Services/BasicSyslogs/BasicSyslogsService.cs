using System.Text;
using backend.Data;
using backend.Extensions;
using backend.Models;
using backend.Services.BasicExceptionlogs;
using backend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services.BasicSyslogs;

/// <summary>異動歷程轉存（Singleton）</summary>
public class BasicSyslogsService(
    IServiceScopeFactory scopeFactory,
    IOptions<BasicSyslogSettings> settings,
    IWebHostEnvironment environment,
    IBasicExceptionlogsService basicExceptionlogsService,
    ILogger<BasicSyslogsService> logger) : IBasicSyslogsService
{
    private const int BatchSize = 500;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(10);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private DateTime? _checkedDate;
    private DateTime _retryAfter = DateTime.MinValue;

    public async Task ArchiveIfDueAsync()
    {
        if (!IsDue())
        {
            return;
        }

        // 已有轉存進行中即略過，不讓使用者的異動等待
        if (!await _lock.WaitAsync(0))
        {
            return;
        }

        try
        {
            if (!IsDue())
            {
                return;
            }

            // 不接受取消：寫檔後、刪除前被中斷會造成下次重複轉存
            await ArchiveAsync(CancellationToken.None);
            _checkedDate = DateTime.Today;
        }
        catch (Exception ex)
        {
            _retryAfter = DateTime.Now.Add(RetryDelay);
            logger.LogError(ex, "系統異動歷程轉存失敗");
            await basicExceptionlogsService.WriteAsync(ex, "BasicSyslogsService.ArchiveIfDueAsync");
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool IsDue() => _checkedDate != DateTime.Today && DateTime.Now >= _retryAfter;

    private async Task ArchiveAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.Today.AddDays(-settings.Value.RetentionDays);

        // 另建範圍，不影響使用者請求中的 DbContext 與交易
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tracker = scope.ServiceProvider.GetRequiredService<BasicSyslogSaveTracker>();

        while (true)
        {
            var batch = await db.BasicSyslog.AsNoTracking()
                .Where(x => x.CreatedAt < cutoff)
                .OrderBy(x => x.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                return;
            }

            foreach (var group in batch.GroupBy(x => (x.CreatedAt.Date, x.MethodName)))
            {
                var path = LogArchive.GetFilePath(environment.ContentRootPath, "Syslog", group.Key.Date, group.Key.MethodName);
                var content = new StringBuilder();
                foreach (var log in group)
                {
                    content.AppendLine($"==== {log.CreatedAt:yyyy-MM-dd HH:mm:ss.fff}　[{log.Action}]　帳號：{log.Username ?? "-"}");
                    if (!string.IsNullOrEmpty(log.Description))
                    {
                        content.AppendLine($"說明：{log.Description}");
                    }

                    content.AppendLine($"異動前：{log.BeforeData}");
                    content.AppendLine($"異動後：{log.AfterData}");
                    content.AppendLine();
                }

                await LogArchive.AppendAsync(path, content.ToString(), cancellationToken);
            }

            // 寫檔成功才刪除
            var ids = batch.Select(x => x.Id).ToList();
            using (tracker.AllowLogArchiveDelete(nameof(BasicSyslog)))
            {
                await db.BasicSyslog.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
            }

            logger.LogInformation("已轉存系統異動歷程 {Count} 筆", batch.Count);
        }
    }
}

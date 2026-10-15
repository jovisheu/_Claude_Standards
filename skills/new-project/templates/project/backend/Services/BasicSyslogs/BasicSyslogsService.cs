using System.Text;
using backend.Data;
using backend.Extensions;
using backend.Models;
using backend.Services.BasicExceptionlogs;
using backend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services.BasicSyslogs;

/// <summary>
/// 系統異動歷程轉存；以 Singleton 註冊，同一時間只有一個請求執行轉存，避免重複寫檔
/// </summary>
public class BasicSyslogsService(
    IServiceScopeFactory scopeFactory,
    IOptions<BasicSyslogSettings> settingsOptions,
    IWebHostEnvironment env,
    IBasicExceptionlogsService basicExceptionlogs,
    ILogger<BasicSyslogsService> logger) : IBasicSyslogsService
{
    private const int ArchiveBatchSize = 1000;
    private const int RetryMinutes = 10;

    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>已確認沒有過期紀錄的日期</summary>
    private DateTime _checkedDate = DateTime.MinValue;

    /// <summary>轉存失敗後，在此時間前不再重試</summary>
    private DateTime _retryAfter = DateTime.MinValue;

    public async Task ArchiveIfDueAsync()
    {
        if (!IsDue(DateTime.Now)) return;

        await _lock.WaitAsync();
        try
        {
            var now = DateTime.Now;
            if (!IsDue(now)) return;

            try
            {
                var count = await ArchiveAsync(now);
                if (count > 0)
                {
                    logger.LogInformation("系統異動歷程已轉存：{Count} 筆", count);
                }
                _checkedDate = now.Date;
            }
            catch (Exception ex)
            {
                // 轉存失敗不影響使用者的異動；舊紀錄保留於資料庫，稍後再試
                _retryAfter = now.AddMinutes(RetryMinutes);
                logger.LogError(ex, "系統異動歷程轉存失敗");
                await basicExceptionlogs.WriteAsync(ex, $"{nameof(BasicSyslogsService)}.{nameof(ArchiveIfDueAsync)}", null);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool IsDue(DateTime now) => _checkedDate != now.Date && now >= _retryAfter;

    /// <summary>
    /// 分批轉存；先寫檔成功才刪除，寫檔失敗時保留資料庫紀錄。
    /// 不接受取消，避免寫檔後、刪除前中斷造成下次重複轉存
    /// </summary>
    private async Task<int> ArchiveAsync(DateTime now)
    {
        var cutoff = now.Date.AddDays(-settingsOptions.Value.RetentionDays);
        var total = 0;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tracker = scope.ServiceProvider.GetRequiredService<BasicSyslogSaveTracker>();

        while (true)
        {
            var batch = await db.BasicSyslog.AsNoTracking()
                .Where(x => x.CreatedAt < cutoff)
                .OrderBy(x => x.Id)
                .Take(ArchiveBatchSize)
                .ToListAsync();
            if (batch.Count == 0) break;

            foreach (var group in batch.GroupBy(x => LogArchive.GetFilePath(env.ContentRootPath, "Syslog", x.CreatedAt, x.MethodName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(group.Key)!);
                var content = new StringBuilder();
                foreach (var item in group)
                {
                    AppendRecord(content, item);
                }
                await File.AppendAllTextAsync(group.Key, content.ToString(), Encoding.UTF8);
            }

            var ids = batch.Select(x => x.Id).ToList();
            // 系統紀錄表已轉存，批次刪除不需異動歷程，明確告知把關
            using (tracker.AllowLogArchiveDelete(nameof(BasicSyslog)))
            {
                await db.BasicSyslog.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
            }
            total += batch.Count;
        }

        return total;
    }

    private static void AppendRecord(StringBuilder content, BasicSyslog item)
    {
        content.AppendLine($"[{item.CreatedAt:yyyy-MM-dd HH:mm:ss.fff}] Id：{item.Id}　類別：{item.Action}　帳號：{item.Username ?? "-"}");
        content.AppendLine($"MethodName：{item.MethodName}");
        if (item.Description != null)
        {
            content.AppendLine($"說明：{item.Description}");
        }
        content.AppendLine($"異動前：{item.BeforeData}");
        content.AppendLine($"異動後：{item.AfterData}");
        content.AppendLine(new string('-', 80));
    }
}

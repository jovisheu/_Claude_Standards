using System.Text;
using backend.Data;
using backend.Extensions;
using backend.Models;
using backend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services.BasicExceptionlogs;

/// <summary>
/// 系統錯誤紀錄；每次寫入另建 DbContext，不沿用發生錯誤的請求中可能含失敗變更的 DbContext
/// </summary>
public class BasicExceptionlogsService(
    IServiceScopeFactory scopeFactory,
    IOptions<BasicExceptionlogSettings> settingsOptions,
    IWebHostEnvironment env,
    ILogger<BasicExceptionlogsService> logger) : IBasicExceptionlogsService
{
    private const int ArchiveBatchSize = 1000;
    private const int MethodNameMaxLength = 200;

    public async Task WriteAsync(Exception exception, string methodName, string? username, string? message = null)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.BasicExceptionlog.Add(new BasicExceptionlog
            {
                ExceptionType = Truncate(exception.GetType().FullName ?? exception.GetType().Name, 200),
                MethodName = Truncate(methodName, MethodNameMaxLength),
                Message = message ?? exception.Message,
                // 含內部例外；訊息被取代時只存呼叫堆疊，避免寫入原始訊息
                StackTrace = message == null ? exception.ToString() : exception.StackTrace,
                Username = string.IsNullOrEmpty(username) ? null : Truncate(username, 50),
                CreatedAt = DateTime.Now
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // 資料庫無法寫入（如連線中斷）時改寫入 Log，不影響原本的錯誤回應
            logger.LogError(ex, "系統錯誤紀錄寫入失敗：{MethodName} {ExceptionType}", methodName, exception.GetType().Name);
        }
    }

    public async Task<int> ArchiveAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.Now.Date.AddDays(-settingsOptions.Value.RetentionDays);
        var total = 0;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tracker = scope.ServiceProvider.GetRequiredService<BasicSyslogSaveTracker>();

        while (true)
        {
            var batch = await db.BasicExceptionlog.AsNoTracking()
                .Where(x => x.CreatedAt < cutoff)
                .OrderBy(x => x.Id)
                .Take(ArchiveBatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0) break;

            // 先寫檔成功才刪除，寫檔失敗時保留資料庫紀錄
            foreach (var group in batch.GroupBy(x => LogArchive.GetFilePath(env.ContentRootPath, "Exception", x.CreatedAt, x.MethodName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(group.Key)!);
                var content = new StringBuilder();
                foreach (var item in group)
                {
                    AppendRecord(content, item);
                }
                await File.AppendAllTextAsync(group.Key, content.ToString(), Encoding.UTF8, cancellationToken);
            }

            var ids = batch.Select(x => x.Id).ToList();
            // 系統紀錄表已轉存，批次刪除不需異動歷程，明確告知把關
            using (tracker.AllowLogArchiveDelete(nameof(BasicExceptionlog)))
            {
                await db.BasicExceptionlog.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
            }
            total += batch.Count;
        }

        return total;
    }

    private static void AppendRecord(StringBuilder content, BasicExceptionlog item)
    {
        content.AppendLine($"[{item.CreatedAt:yyyy-MM-dd HH:mm:ss.fff}] Id：{item.Id}　帳號：{item.Username ?? "-"}");
        content.AppendLine($"MethodName：{item.MethodName}");
        content.AppendLine($"ExceptionType：{item.ExceptionType}");
        content.AppendLine($"Message：{item.Message}");
        if (!string.IsNullOrEmpty(item.StackTrace))
        {
            content.AppendLine("StackTrace：");
            content.AppendLine(item.StackTrace);
        }
        content.AppendLine(new string('-', 80));
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}

using System.Text;
using backend.Data;
using backend.Extensions;
using backend.Models;
using backend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services.BasicExceptionlogs;

/// <summary>系統錯誤紀錄（Singleton）</summary>
public class BasicExceptionlogsService(
    IServiceScopeFactory scopeFactory,
    IOptions<BasicExceptionlogSettings> settings,
    IWebHostEnvironment environment,
    ILogger<BasicExceptionlogsService> logger) : IBasicExceptionlogsService
{
    private const int BatchSize = 500;
    private const int MethodNameMaxLength = 200;
    private const int ExceptionTypeMaxLength = 200;
    private const int UsernameMaxLength = 50;

    public async Task WriteAsync(Exception exception, string methodName, string? username = null, string? message = null)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.BasicExceptionlog.Add(new BasicExceptionlog
            {
                ExceptionType = Truncate(exception.GetType().FullName ?? exception.GetType().Name, ExceptionTypeMaxLength),
                MethodName = Truncate(methodName, MethodNameMaxLength),
                Message = message ?? exception.Message,
                StackTrace = BuildStackTrace(exception, includeInnerMessage: message is null),
                Username = username is null ? null : Truncate(username, UsernameMaxLength),
                CreatedAt = DateTime.Now,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "寫入系統錯誤紀錄失敗：{MethodName}", methodName);
        }
    }

    public async Task ArchiveAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.Today.AddDays(-settings.Value.RetentionDays);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tracker = scope.ServiceProvider.GetRequiredService<BasicSyslogSaveTracker>();

        while (true)
        {
            var batch = await db.BasicExceptionlog.AsNoTracking()
                .Where(x => x.CreatedAt < cutoff)
                .OrderBy(x => x.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            foreach (var group in batch.GroupBy(x => (x.CreatedAt.Date, x.MethodName)))
            {
                var path = LogArchive.GetFilePath(environment.ContentRootPath, "Exception", group.Key.Date, group.Key.MethodName);
                var content = new StringBuilder();
                foreach (var log in group)
                {
                    content.AppendLine($"==== {log.CreatedAt:yyyy-MM-dd HH:mm:ss.fff}　帳號：{log.Username ?? "-"}");
                    content.AppendLine($"類別：{log.ExceptionType}");
                    content.AppendLine($"訊息：{log.Message}");
                    content.AppendLine(log.StackTrace ?? string.Empty);
                    content.AppendLine();
                }

                await LogArchive.AppendAsync(path, content.ToString(), cancellationToken);
            }

            // 寫檔成功才刪除
            var ids = batch.Select(x => x.Id).ToList();
            using (tracker.AllowLogArchiveDelete(nameof(BasicExceptionlog)))
            {
                await db.BasicExceptionlog.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
            }

            logger.LogInformation("已轉存系統錯誤紀錄 {Count} 筆", batch.Count);
        }
    }

    private static string BuildStackTrace(Exception exception, bool includeInnerMessage)
    {
        var builder = new StringBuilder(exception.StackTrace);
        var inner = exception.InnerException;
        while (inner is not null)
        {
            builder.AppendLine();
            builder.Append("--- 內部例外：").Append(inner.GetType().FullName);
            if (includeInnerMessage)
            {
                builder.Append("：").Append(inner.Message);
            }

            builder.AppendLine();
            builder.Append(inner.StackTrace);
            inner = inner.InnerException;
        }

        return builder.ToString();
    }

    private static string Truncate(string value, int maxLength) => value.Length > maxLength ? value[..maxLength] : value;
}

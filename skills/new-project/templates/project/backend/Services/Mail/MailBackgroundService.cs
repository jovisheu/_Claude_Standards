using backend.Services.BasicExceptionlogs;

namespace backend.Services.Mail;

/// <summary>郵件背景寄送；失敗時寫入紀錄（不含收件人與內文），不中斷服務</summary>
public class MailBackgroundService(
    MailQueue queue,
    IMailService mailService,
    IBasicExceptionlogsService basicExceptionlogsService,
    ILogger<MailBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var message in queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await mailService.SendAsync(message, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    // 例外訊息可能含收件人，以固定訊息取代
                    logger.LogError("郵件寄送失敗：{Category}（{ExceptionType}）", message.Category, ex.GetType().Name);
                    await basicExceptionlogsService.WriteAsync(ex, "MailBackgroundService.ExecuteAsync", message: $"郵件寄送失敗：{message.Category}");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // 應用程式停止
        }
    }
}

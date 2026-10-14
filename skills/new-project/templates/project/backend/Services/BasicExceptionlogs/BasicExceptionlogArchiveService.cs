namespace backend.Services.BasicExceptionlogs;

/// <summary>
/// 背景轉存系統錯誤紀錄：啟動時執行一次，之後每天一次（IIS 回收或閒置關閉後，下次啟動時補做）
/// </summary>
public class BasicExceptionlogArchiveService(
    IBasicExceptionlogsService basicExceptionlogs,
    ILogger<BasicExceptionlogArchiveService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        try
        {
            do
            {
                try
                {
                    var count = await basicExceptionlogs.ArchiveAsync(stoppingToken);
                    if (count > 0)
                    {
                        logger.LogInformation("系統錯誤紀錄已轉存：{Count} 筆", count);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // 背景作業失敗：寫入 Log 與 BasicExceptionlog（資料庫無法寫入時 WriteAsync 只寫 Log，不丟出例外）
                    logger.LogError(ex, "系統錯誤紀錄轉存失敗");
                    await basicExceptionlogs.WriteAsync(ex, $"{nameof(BasicExceptionlogArchiveService)}.{nameof(ExecuteAsync)}", null);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // 應用程式關閉
        }
    }
}

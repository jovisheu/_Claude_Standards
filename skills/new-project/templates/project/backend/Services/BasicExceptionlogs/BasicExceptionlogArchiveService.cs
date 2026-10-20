namespace backend.Services.BasicExceptionlogs;

/// <summary>
/// 系統錯誤紀錄轉存背景服務：啟動時執行一次，之後每天一次；失敗時寫入紀錄，不中斷服務
/// </summary>
public class BasicExceptionlogArchiveService(
    IBasicExceptionlogsService basicExceptionlogsService,
    ILogger<BasicExceptionlogArchiveService> logger) : BackgroundService
{
    private const string MethodName = "BasicExceptionlogArchiveService.ExecuteAsync";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ArchiveAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ArchiveAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // 應用程式停止
        }
    }

    private async Task ArchiveAsync(CancellationToken stoppingToken)
    {
        try
        {
            await basicExceptionlogsService.ArchiveAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // 應用程式停止
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "系統錯誤紀錄轉存失敗");
            await basicExceptionlogsService.WriteAsync(ex, MethodName);
        }
    }
}

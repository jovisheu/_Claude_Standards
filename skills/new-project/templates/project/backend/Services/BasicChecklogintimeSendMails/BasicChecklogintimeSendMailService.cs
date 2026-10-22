using backend.Data;
using backend.Models;
using backend.Services.BasicExceptionlogs;
using backend.Services.Mail;
using backend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services.BasicChecklogintimeSendMails;

/// <summary>
/// 密碼即將到期通知背景服務：啟動時執行一次，之後每小時檢查一次；
/// 到期日前 ChecklogintimeSendMailDay 天起至到期為止，每位啟用中的使用者每日寄送一封，當日已寄送則不再寄送，不補寄過去的日期。
/// 寄送成功才寫入 BasicChecklogintimeSendMailLog，失敗時寫入系統錯誤紀錄，下次檢查重試。
/// </summary>
public class BasicChecklogintimeSendMailService(
    IServiceScopeFactory scopeFactory,
    IMailService mailService,
    IBasicExceptionlogsService basicExceptionlogsService,
    IOptions<SecuritySettings> settings,
    ILogger<BasicChecklogintimeSendMailService> logger) : BackgroundService
{
    private const string MethodName = "BasicChecklogintimeSendMailService.ExecuteAsync";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (settings.Value.Password.ChecklogintimeSendMailDay <= 0)
        {
            return;
        }

        await SendAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SendAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // 應用程式停止
        }
    }

    private async Task SendAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordSettings = settings.Value.Password;
            var now = DateTime.Now;
            var today = now.Date;

            // 到期日前 N 天（含）起進入通知區間：最後變更日 + ExpireDays - N <= 今天，即最後變更時間早於 今天 + N - ExpireDays + 1 天；
            // 已逾期者登入時會強制變更，不再通知；無密碼歷程者 LastChangedAt 為 null，不列入
            var noticeBefore = today.AddDays(passwordSettings.ChecklogintimeSendMailDay - passwordSettings.ExpireDays + 1);
            var expiredBefore = now.AddDays(-passwordSettings.ExpireDays);
            var candidates = await db.BasicUser.AsNoTracking()
                .Where(u => u.IsActive)
                .Select(u => new
                {
                    u.Username,
                    u.Email,
                    u.DisplayName,
                    LastChangedAt = db.BasicPwdlog.Where(p => p.Username == u.Username).Max(p => (DateTime?)p.CreatedAt),
                })
                .Where(x => x.LastChangedAt < noticeBefore && x.LastChangedAt > expiredBefore)
                .Where(x => !db.BasicChecklogintimeSendMailLog.Any(l => l.Username == x.Username && l.CreatedAt >= today))
                .ToListAsync(stoppingToken);

            foreach (var candidate in candidates)
            {
                stoppingToken.ThrowIfCancellationRequested();
                var expireAt = candidate.LastChangedAt!.Value.AddDays(passwordSettings.ExpireDays);
                var remainingDays = Math.Max((expireAt.Date - today).Days, 0);
                var message = mailService.CreatePasswordExpireNoticeMail(candidate.Email, candidate.DisplayName, candidate.Username, expireAt, remainingDays);
                try
                {
                    await mailService.SendAsync(message, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    // 例外訊息可能含收件人，以固定訊息取代
                    logger.LogError("郵件寄送失敗：{Category}（{ExceptionType}）", message.Category, ex.GetType().Name);
                    await basicExceptionlogsService.WriteAsync(ex, MethodName, message: $"郵件寄送失敗：{message.Category}");
                    continue;
                }

                db.BasicChecklogintimeSendMailLog.Add(new BasicChecklogintimeSendMailLog
                {
                    Username = candidate.Username,
                    Email = candidate.Email,
                    CreatedAt = DateTime.Now,
                });
                await db.SaveChangesAsync(stoppingToken);
                logger.LogInformation("已寄送密碼即將到期通知：{Username}", candidate.Username);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // 應用程式停止
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "密碼即將到期通知處理失敗");
            await basicExceptionlogsService.WriteAsync(ex, MethodName);
        }
    }
}

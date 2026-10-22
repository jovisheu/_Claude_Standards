using backend.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace backend.Services.Mail;

/// <summary>
/// 郵件服務（Singleton，使用 MailKit）；Smtp:Enabled 為 false 時寫入 PickupDirectory（.eml）不寄出。
/// Log 不記錄收件人與內文。
/// </summary>
public class MailService(
    IOptions<SmtpSettings> smtpSettings,
    IOptions<AppSettings> appSettings,
    MailTemplateStore templateStore,
    MailQueue queue,
    IWebHostEnvironment environment,
    ILogger<MailService> logger) : IMailService
{
    private const string ResetPasswordCategory = "重設密碼";
    private const string PasswordExpireNoticeCategory = "密碼即將到期通知";

    public void QueueResetPasswordMail(string email, string displayName, string resetUrl, int expireMinutes)
    {
        var systemName = appSettings.Value.SystemName;
        var body = templateStore.Render(MailTemplateStore.ResetPassword, new Dictionary<string, string>
        {
            ["SystemName"] = systemName,
            ["DisplayName"] = displayName,
            ["ResetUrl"] = resetUrl,
            ["ExpireMinutes"] = expireMinutes.ToString(),
        });

        if (!queue.TryEnqueue(new MailMessageItem(email, displayName, $"【{systemName}】重設密碼通知", body, ResetPasswordCategory)))
        {
            logger.LogWarning("郵件佇列已滿，捨棄一封郵件：{Category}", ResetPasswordCategory);
        }
    }

    public MailMessageItem CreatePasswordExpireNoticeMail(string email, string displayName, string username, DateTime expireAt, int remainingDays)
    {
        var systemName = appSettings.Value.SystemName;
        var body = templateStore.Render(MailTemplateStore.PasswordExpireNotice, new Dictionary<string, string>
        {
            ["SystemName"] = systemName,
            ["DisplayName"] = displayName,
            ["Username"] = username,
            ["ExpireAt"] = expireAt.ToString("yyyy-MM-dd HH:mm"),
            ["RemainingDays"] = remainingDays.ToString(),
            ["LoginUrl"] = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/login",
        });

        return new MailMessageItem(email, displayName, $"【{systemName}】密碼即將到期通知", body, PasswordExpireNoticeCategory);
    }

    public async Task SendAsync(MailMessageItem item, CancellationToken cancellationToken)
    {
        var settings = smtpSettings.Value;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderEmail));
        message.To.Add(new MailboxAddress(item.ToName, item.ToEmail));
        message.Subject = item.Subject;
        message.Body = new BodyBuilder { HtmlBody = item.HtmlBody }.ToMessageBody();

        if (!settings.Enabled)
        {
            var directory = Path.Combine(environment.ContentRootPath, settings.PickupDirectory);
            Directory.CreateDirectory(directory);
            var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}.eml";
            await message.WriteToAsync(Path.Combine(directory, fileName), cancellationToken);
            return;
        }

        // 465 為隱含 TLS，其餘 SSL 連線使用 STARTTLS
        using var client = new SmtpClient();
        var socketOptions = settings.UseSsl
            ? (settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
            : SecureSocketOptions.None;
        await client.ConnectAsync(settings.Host, settings.Port, socketOptions, cancellationToken);
        if (!string.IsNullOrEmpty(settings.Username))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        logger.LogInformation("郵件已寄送：{Category}", item.Category);
    }
}

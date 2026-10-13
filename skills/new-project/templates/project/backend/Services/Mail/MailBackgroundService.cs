using backend.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace backend.Services.Mail;

/// <summary>
/// 背景寄送郵件；Smtp:Enabled = false 時改寫入 PickupDirectory
/// </summary>
public class MailBackgroundService(
    MailQueue queue,
    IOptions<SmtpSettings> smtpOptions,
    IWebHostEnvironment env,
    ILogger<MailBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var item in queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await SendAsync(item, stoppingToken);
                    logger.LogInformation("郵件已寄送：{Category}", item.Category);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // 不記錄收件人與內文，例外訊息可能含收件人，只記錄型別
                    logger.LogError("郵件寄送失敗：{Category} {ExceptionType}", item.Category, ex.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 應用程式關閉
        }
    }

    private async Task SendAsync(MailItem item, CancellationToken cancellationToken)
    {
        var settings = smtpOptions.Value;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderEmail));
        message.To.Add(new MailboxAddress(item.ToName, item.ToEmail));
        message.Subject = item.Subject;
        message.Body = new BodyBuilder { HtmlBody = item.HtmlBody }.ToMessageBody();

        if (!settings.Enabled)
        {
            var directory = Path.Combine(env.ContentRootPath, settings.PickupDirectory);
            Directory.CreateDirectory(directory);
            var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}.eml";
            await message.WriteToAsync(Path.Combine(directory, fileName), cancellationToken);
            return;
        }

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
    }
}

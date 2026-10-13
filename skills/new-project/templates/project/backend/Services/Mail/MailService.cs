using System.Net;
using backend.Settings;
using Microsoft.Extensions.Options;

namespace backend.Services.Mail;

public class MailService(MailQueue queue, MailTemplateStore templates, IOptions<AppSettings> appOptions, ILogger<MailService> logger) : IMailService
{
    public void QueueResetPasswordMail(string toEmail, string displayName, string resetLink, int expireMinutes)
    {
        var systemName = appOptions.Value.SystemName;

        // 放入 HTML 的動態資料一律先編碼
        var body = templates.Get(MailTemplateStore.ResetPassword)
            .Replace("{{SystemName}}", WebUtility.HtmlEncode(systemName))
            .Replace("{{DisplayName}}", WebUtility.HtmlEncode(displayName))
            .Replace("{{ResetLink}}", WebUtility.HtmlEncode(resetLink))
            .Replace("{{ExpireMinutes}}", expireMinutes.ToString());

        var item = new MailItem(toEmail, displayName, $"[{systemName}] 重設密碼通知", body, "重設密碼");
        if (!queue.TryEnqueue(item))
        {
            // 不記錄收件人與內文
            logger.LogWarning("郵件佇列已滿，未寄送：{Category}", item.Category);
        }
    }
}

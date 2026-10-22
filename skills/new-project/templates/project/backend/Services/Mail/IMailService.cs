namespace backend.Services.Mail;

public interface IMailService
{
    /// <summary>將重設密碼郵件排入背景寄送（不寄送密碼，只寄一次性重設連結）</summary>
    void QueueResetPasswordMail(string email, string displayName, string resetUrl, int expireMinutes);

    /// <summary>產生密碼即將到期通知（不排入佇列，由呼叫端以 SendAsync 寄送，寄送成功才寫入紀錄）</summary>
    MailMessageItem CreatePasswordExpireNoticeMail(string email, string displayName, string username, DateTime expireAt, int remainingDays);

    /// <summary>實際寄送（由背景服務呼叫）</summary>
    Task SendAsync(MailMessageItem message, CancellationToken cancellationToken);
}

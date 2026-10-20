namespace backend.Services.Mail;

public interface IMailService
{
    /// <summary>將重設密碼郵件排入背景寄送（不寄送密碼，只寄一次性重設連結）</summary>
    void QueueResetPasswordMail(string email, string displayName, string resetUrl, int expireMinutes);

    /// <summary>實際寄送（由背景服務呼叫）</summary>
    Task SendAsync(MailMessageItem message, CancellationToken cancellationToken);
}

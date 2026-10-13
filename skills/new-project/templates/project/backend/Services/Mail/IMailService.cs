namespace backend.Services.Mail;

public interface IMailService
{
    /// <summary>
    /// 排入重設密碼郵件，由背景服務寄送（避免以回應時間探測帳號）
    /// </summary>
    void QueueResetPasswordMail(string toEmail, string displayName, string resetLink, int expireMinutes);
}

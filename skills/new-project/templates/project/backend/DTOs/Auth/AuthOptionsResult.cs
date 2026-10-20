namespace backend.DTOs.Auth;

/// <summary>前端提示用的安全參數（不在前端寫死）</summary>
public class AuthOptionsResult
{
    public bool CaptchaEnabled { get; set; }

    public int CaptchaLength { get; set; }

    public int PasswordMinLength { get; set; }

    public int PasswordRequiredCategoryCount { get; set; }

    public int PasswordHistoryCount { get; set; }

    public int PasswordExpireDays { get; set; }

    public int PasswordResetTokenExpireMinutes { get; set; }
}

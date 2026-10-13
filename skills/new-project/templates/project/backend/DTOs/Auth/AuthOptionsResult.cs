namespace backend.DTOs.Auth;

/// <summary>
/// 前端提示用的安全參數（由後端提供，不在前端寫死）
/// </summary>
public class AuthOptionsResult
{
    public string SystemName { get; set; } = string.Empty;
    public bool CaptchaEnabled { get; set; }
    public int CaptchaLength { get; set; }
    public int PasswordMinLength { get; set; }
    public int PasswordRequiredCategories { get; set; }
    public int PasswordHistoryCount { get; set; }
    public int PasswordExpireDays { get; set; }
    public int ResetTokenExpireMinutes { get; set; }
}

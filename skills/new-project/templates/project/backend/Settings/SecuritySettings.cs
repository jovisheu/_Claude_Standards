namespace backend.Settings;

/// <summary>
/// 安全參數（皆取自 appsettings.json，不寫死在程式中）
/// </summary>
public class SecuritySettings
{
    public const string SectionName = "Security";

    public LoginSettings Login { get; set; } = new();
    public PasswordSettings Password { get; set; } = new();
    public PasswordResetSettings PasswordReset { get; set; } = new();
}

public class LoginSettings
{
    /// <summary>期間內允許的錯誤次數，達到即鎖定</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>計算錯誤次數的期間（分鐘）</summary>
    public int FailedWindowMinutes { get; set; } = 15;

    /// <summary>鎖定時間（分鐘）</summary>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>登入相關端點每 IP 每分鐘請求上限</summary>
    public int RateLimitPerMinute { get; set; } = 10;

    public bool CaptchaEnabled { get; set; } = true;
    public int CaptchaLength { get; set; } = 5;
    public int CaptchaExpireMinutes { get; set; } = 5;

    /// <summary>驗證碼端點每 IP 每分鐘請求上限</summary>
    public int CaptchaRateLimitPerMinute { get; set; } = 30;
}

public class PasswordSettings
{
    public int MinLength { get; set; } = 8;

    /// <summary>大寫、小寫、數字、特殊符號至少須符合的類別數</summary>
    public int RequiredCategories { get; set; } = 3;

    /// <summary>不得與前 N 次密碼相同</summary>
    public int HistoryCount { get; set; } = 3;

    /// <summary>密碼有效天數</summary>
    public int ExpireDays { get; set; } = 180;

    /// <summary>逾期變更憑證有效時間（分鐘）</summary>
    public int ChangeTokenExpireMinutes { get; set; } = 10;
}

public class PasswordResetSettings
{
    /// <summary>重設連結有效時間（分鐘）</summary>
    public int TokenExpireMinutes { get; set; } = 30;

    /// <summary>同帳號申請間隔（分鐘）</summary>
    public int RequestIntervalMinutes { get; set; } = 5;
}

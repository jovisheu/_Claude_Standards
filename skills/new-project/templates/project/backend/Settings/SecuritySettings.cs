namespace backend.Settings;

/// <summary>
/// 安全參數（appsettings.json 的 Security 區段）；前端提示的數值一律由後端提供
/// </summary>
public class SecuritySettings
{
    public const string SectionName = "Security";

    public LoginSettings Login { get; set; } = new();

    public PasswordSettings Password { get; set; } = new();

    public PasswordResetSettings PasswordReset { get; set; } = new();

    public RateLimitSettings RateLimit { get; set; } = new();
}

public class LoginSettings
{
    /// <summary>判斷鎖定的錯誤次數</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>計算錯誤次數的分鐘數</summary>
    public int FailedWindowMinutes { get; set; } = 15;

    /// <summary>鎖定分鐘數（自最後一次錯誤起算）</summary>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>是否使用驗證碼</summary>
    public bool CaptchaEnabled { get; set; } = true;

    /// <summary>驗證碼字元數</summary>
    public int CaptchaLength { get; set; } = 5;

    /// <summary>驗證碼有效分鐘數</summary>
    public int CaptchaExpireMinutes { get; set; } = 5;
}

public class PasswordSettings
{
    /// <summary>最少字元數</summary>
    public int MinLength { get; set; } = 8;

    /// <summary>大寫、小寫、數字、特殊符號至少須符合的種類數</summary>
    public int RequiredCategoryCount { get; set; } = 3;

    /// <summary>不得與前幾次密碼相同</summary>
    public int HistoryCount { get; set; } = 3;

    /// <summary>密碼有效天數</summary>
    public int ExpireDays { get; set; } = 180;

    /// <summary>逾期變更密碼憑證的有效分鐘數</summary>
    public int ChangeTokenExpireMinutes { get; set; } = 10;
}

public class PasswordResetSettings
{
    /// <summary>重設連結有效分鐘數</summary>
    public int TokenExpireMinutes { get; set; } = 10;

    /// <summary>同帳號申請間隔分鐘數</summary>
    public int RequestIntervalMinutes { get; set; } = 5;
}

public class RateLimitSettings
{
    /// <summary>登入等驗證端點每個 IP 於時間窗內的請求上限</summary>
    public int AuthPermitLimit { get; set; } = 10;

    /// <summary>驗證碼端點每個 IP 於時間窗內的請求上限</summary>
    public int CaptchaPermitLimit { get; set; } = 30;

    /// <summary>時間窗秒數</summary>
    public int WindowSeconds { get; set; } = 60;
}

namespace backend.Extensions;

/// <summary>
/// 速率限制 Policy 名稱
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>登入、忘記密碼、重設密碼等端點（每 IP）</summary>
    public const string Login = "login";

    /// <summary>驗證碼端點（每 IP）</summary>
    public const string Captcha = "captcha";
}

namespace backend.DTOs.Auth;

/// <summary>
/// 登入結果；密碼逾期時不發 JWT，改發一次性變更憑證
/// </summary>
public class LoginResult
{
    public bool RequirePasswordChange { get; set; }

    /// <summary>逾期變更憑證（一次性、限時）</summary>
    public string? ChangeToken { get; set; }

    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public CurrentUserResult? User { get; set; }
}

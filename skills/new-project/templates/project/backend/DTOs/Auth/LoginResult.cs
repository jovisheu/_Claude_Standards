namespace backend.DTOs.Auth;

public class LoginResult
{
    /// <summary>密碼已逾期，須先變更密碼（此時不發 JWT）</summary>
    public bool RequirePasswordChange { get; set; }

    /// <summary>逾期變更密碼的一次性憑證</summary>
    public string? PasswordChangeToken { get; set; }

    public string? Token { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public CurrentBasicUserResult? BasicUser { get; set; }
}

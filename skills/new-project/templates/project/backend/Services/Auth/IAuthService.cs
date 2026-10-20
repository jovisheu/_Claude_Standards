using backend.DTOs.Auth;

namespace backend.Services.Auth;

public interface IAuthService
{
    AuthOptionsResult GetOptions();

    Task<LoginResult> LoginAsync(LoginRequest request);

    Task ChangeExpiredPasswordAsync(ChangeExpiredPasswordRequest request);

    /// <summary>申請忘記密碼；不論帳號與 Email 是否相符都不丟出例外（回相同訊息）</summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request);

    Task ResetPasswordAsync(ResetPasswordRequest request);

    Task<CurrentBasicUserResult> GetCurrentBasicUserAsync(int basicUserId);
}

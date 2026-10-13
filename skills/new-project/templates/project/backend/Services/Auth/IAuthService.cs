using backend.DTOs.Auth;

namespace backend.Services.Auth;

public interface IAuthService
{
    AuthOptionsResult GetOptions();
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task ChangeExpiredPasswordAsync(ChangeExpiredPasswordRequest request);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task<CurrentUserResult> GetCurrentUserAsync(int userId);
}

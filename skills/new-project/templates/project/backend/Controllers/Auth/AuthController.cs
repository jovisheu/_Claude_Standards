using backend.DTOs;
using backend.DTOs.Auth;
using backend.Extensions;
using backend.Services.Auth;
using backend.Services.Captcha;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace backend.Controllers.Auth;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController(IAuthService authService, ICaptchaService captchaService) : ControllerBase
{
    /// <summary>前端提示用的安全參數</summary>
    [HttpGet("options")]
    [AllowAnonymous]
    public ApiResponse<AuthOptionsResult> GetOptions()
        => ApiResponse<AuthOptionsResult>.Ok(authService.GetOptions());

    /// <summary>取得驗證碼圖片</summary>
    [HttpGet("captcha")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Captcha)]
    public ApiResponse<CaptchaResult> GetCaptcha()
        => ApiResponse<CaptchaResult>.Ok(captchaService.Create());

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ApiResponse<LoginResult>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return ApiResponse<LoginResult>.Ok(result, result.RequirePasswordChange ? "密碼已逾期，請先變更密碼" : "登入成功");
    }

    /// <summary>密碼逾期時以變更憑證設定新密碼</summary>
    [HttpPost("change-expired-password")]
    [AllowAnonymous]
    [BasicSyslogDescription("逾期變更密碼")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ApiResponse> ChangeExpiredPassword(ChangeExpiredPasswordRequest request)
    {
        await authService.ChangeExpiredPasswordAsync(request);
        return ApiResponse.Success("密碼已變更，請以新密碼重新登入");
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [BasicSyslogDescription("申請重設密碼")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ApiResponse> ForgotPassword(ForgotPasswordRequest request)
    {
        await authService.ForgotPasswordAsync(request);
        return ApiResponse.Success("若帳號與 Email 相符，系統將寄送重設密碼連結至該信箱，請於時限內完成設定");
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [BasicSyslogDescription("重設密碼")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ApiResponse> ResetPassword(ResetPasswordRequest request)
    {
        await authService.ResetPasswordAsync(request);
        return ApiResponse.Success("密碼已重設，請以新密碼重新登入");
    }

    /// <summary>目前登入者資訊與權限（即時取自資料庫）</summary>
    [HttpGet("me")]
    public async Task<ApiResponse<CurrentBasicUserResult>> Me()
        => ApiResponse<CurrentBasicUserResult>.Ok(await authService.GetCurrentBasicUserAsync(User.GetBasicUserId()));
}

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
    public ActionResult<ApiResponse<AuthOptionsResult>> Options()
        => Ok(ApiResponse<AuthOptionsResult>.Success(authService.GetOptions()));

    [HttpGet("captcha")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Captcha)]
    public ActionResult<ApiResponse<CaptchaResult>> Captcha()
        => Ok(ApiResponse<CaptchaResult>.Success(captchaService.Create()));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<ActionResult<ApiResponse<LoginResult>>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        var message = result.RequirePasswordChange ? "密碼已逾期，請先變更密碼" : "登入成功";
        return Ok(ApiResponse<LoginResult>.Success(result, message));
    }

    [HttpPost("change-expired-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [BasicSyslogDescription("逾期變更密碼")]
    public async Task<ActionResult<ApiResponse>> ChangeExpiredPassword(ChangeExpiredPasswordRequest request)
    {
        await authService.ChangeExpiredPasswordAsync(request);
        return Ok(ApiResponse.Ok("密碼已變更，請以新密碼重新登入"));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [BasicSyslogDescription("申請忘記密碼")]
    public async Task<ActionResult<ApiResponse>> ForgotPassword(ForgotPasswordRequest request)
    {
        await authService.ForgotPasswordAsync(request);

        // 不論帳號與 Email 是否相符都回相同訊息
        return Ok(ApiResponse.Ok("若帳號與 Email 相符，系統將寄送重設密碼連結至您的信箱，請於時效內完成設定"));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [BasicSyslogDescription("重設密碼")]
    public async Task<ActionResult<ApiResponse>> ResetPassword(ResetPasswordRequest request)
    {
        await authService.ResetPasswordAsync(request);
        return Ok(ApiResponse.Ok("密碼已重設，請以新密碼重新登入"));
    }

    /// <summary>目前登入者與權限（查資料庫）</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentBasicUserResult>>> Me()
        => Ok(ApiResponse<CurrentBasicUserResult>.Success(await authService.GetCurrentBasicUserAsync(User.GetBasicUserId())));
}

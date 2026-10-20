using System.Security.Cryptography;
using System.Text;
using backend.Data;
using backend.DTOs.Auth;
using backend.Exceptions;
using backend.Models;
using backend.Services.BasicPermissions;
using backend.Services.Captcha;
using backend.Services.Mail;
using backend.Services.Password;
using backend.Services.Token;
using backend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services.Auth;

public class AuthService(
    AppDbContext db,
    IPasswordService passwordService,
    ITokenService tokenService,
    ICaptchaService captchaService,
    IBasicPermissionsService basicPermissionsService,
    IMailService mailService,
    IOptions<SecuritySettings> securitySettings,
    IOptions<AppSettings> appSettings,
    ILogger<AuthService> logger) : IAuthService
{
    private const string InvalidCredentialsMessage = "帳號或密碼錯誤";

    // 帳號不存在時仍執行一次 BCrypt 驗證，避免以回應時間判斷帳號是否存在
    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), 12);

    public AuthOptionsResult GetOptions()
    {
        var settings = securitySettings.Value;
        return new AuthOptionsResult
        {
            CaptchaEnabled = settings.Login.CaptchaEnabled,
            CaptchaLength = settings.Login.CaptchaLength,
            PasswordMinLength = settings.Password.MinLength,
            PasswordRequiredCategoryCount = settings.Password.RequiredCategoryCount,
            PasswordHistoryCount = settings.Password.HistoryCount,
            PasswordExpireDays = settings.Password.ExpireDays,
            PasswordResetTokenExpireMinutes = settings.PasswordReset.TokenExpireMinutes,
        };
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        var login = securitySettings.Value.Login;
        var username = request.Username.Trim();

        // 錯誤紀錄與鎖定一律以小寫帳號判斷，避免以大小寫變化繞過鎖定（不依賴資料庫定序）
        var lockKey = username.ToLowerInvariant();

        // 驗證碼錯誤不計入錯誤次數
        if (login.CaptchaEnabled && !captchaService.Validate(request.CaptchaId, request.CaptchaCode))
        {
            throw new BusinessException("驗證碼錯誤或已過期，請重新輸入");
        }

        // 鎖定期間不驗證密碼，也不再寫入紀錄
        var lockedMinutes = await GetLockedMinutesAsync(lockKey, login);
        if (lockedMinutes > 0)
        {
            throw new BusinessException($"登入失敗次數過多，請於 {lockedMinutes} 分鐘後再試");
        }

        var basicUser = await db.BasicUser.FirstOrDefaultAsync(u => u.Username == username);
        var passwordValid = passwordService.Verify(request.Password, basicUser?.PasswordHash ?? DummyPasswordHash);
        if (basicUser is null || !passwordValid || !basicUser.IsActive)
        {
            // 帳號不存在也記錄；回應不透露帳號是否存在
            db.BasicLoginErrlog.Add(new BasicLoginErrlog { Username = lockKey, CreatedAt = DateTime.Now });
            await db.SaveChangesAsync();
            logger.LogWarning("登入失敗：{Username}", username);
            throw new BusinessException(InvalidCredentialsMessage);
        }

        // 登入失敗紀錄為安全事件，登入成功後不刪除（保留至轉存），鎖定以最近 N 分鐘內的次數判斷

        // 密碼逾期：不發 JWT，改發一次性、限時的變更憑證
        if (await passwordService.IsExpiredAsync(basicUser.Username))
        {
            logger.LogInformation("密碼已逾期，要求變更：{Username}", basicUser.Username);
            return new LoginResult
            {
                RequirePasswordChange = true,
                PasswordChangeToken = tokenService.CreatePasswordChangeToken(basicUser.Id),
            };
        }

        var (token, expiresAt) = tokenService.CreateAccessToken(basicUser);
        logger.LogInformation("登入成功：{Username}", basicUser.Username);
        return new LoginResult
        {
            Token = token,
            ExpiresAt = expiresAt,
            BasicUser = await GetCurrentBasicUserAsync(basicUser.Id),
        };
    }

    public async Task ChangeExpiredPasswordAsync(ChangeExpiredPasswordRequest request)
    {
        var basicUserId = tokenService.GetPasswordChangeBasicUserId(request.Token)
            ?? throw new BusinessException("變更憑證無效或已過期，請重新登入");

        var basicUser = await db.BasicUser.FirstOrDefaultAsync(u => u.Id == basicUserId && u.IsActive)
            ?? throw new BusinessException("變更憑證無效或已過期，請重新登入");

        // 新密碼不符規則時丟出例外，憑證不消耗
        await passwordService.SetPasswordAsync(basicUser, request.NewPassword);
        await db.SaveChangesAsync();

        tokenService.RevokePasswordChangeToken(request.Token);
        logger.LogInformation("逾期變更密碼完成：{Username}", basicUser.Username);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        // 驗證碼在比對帳號與 Email 之前檢查，錯誤時不寄信（與登入共用 CaptchaEnabled）
        if (securitySettings.Value.Login.CaptchaEnabled && !captchaService.Validate(request.CaptchaId, request.CaptchaCode))
        {
            throw new BusinessException("驗證碼錯誤或已過期，請重新輸入");
        }

        var resetSettings = securitySettings.Value.PasswordReset;
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        var basicUser = await db.BasicUser.FirstOrDefaultAsync(u => u.Username == username && u.Email == email && u.IsActive);
        if (basicUser is null)
        {
            logger.LogInformation("忘記密碼申請不符：{Username}", username);
            return;
        }

        // 同帳號限制申請頻率，避免被利用大量寄信（上次申請時間 = 到期時間 - 連結時效）
        var now = DateTime.Now;
        if (basicUser.PasswordResetExpiresAt is { } expiresAt
            && expiresAt.AddMinutes(-resetSettings.TokenExpireMinutes).AddMinutes(resetSettings.RequestIntervalMinutes) > now)
        {
            logger.LogInformation("忘記密碼申請過於頻繁：{Username}", username);
            return;
        }

        // 重新申請即覆蓋，舊連結隨即失效；不變更或停用原密碼。只異動機密欄位，不更新 UpdatedAt
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        basicUser.PasswordResetTokenHash = HashToken(token);
        basicUser.PasswordResetExpiresAt = now.AddMinutes(resetSettings.TokenExpireMinutes);
        await db.SaveChangesAsync();

        // 連結網址一律取自設定檔，不使用請求的 Host 標頭
        var resetUrl = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
        mailService.QueueResetPasswordMail(basicUser.Email, basicUser.DisplayName, resetUrl, resetSettings.TokenExpireMinutes);
        logger.LogInformation("已排入重設密碼郵件：{Username}", username);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var tokenHash = HashToken(request.Token);
        var now = DateTime.Now;
        var basicUser = await db.BasicUser.FirstOrDefaultAsync(u =>
                u.PasswordResetTokenHash == tokenHash && u.PasswordResetExpiresAt > now && u.IsActive)
            ?? throw new BusinessException("重設連結無效或已過期，請重新申請");

        // 新密碼不符規則時丟出例外，連結不消耗；設定成功後由 SetPasswordAsync 作廢連結
        await passwordService.SetPasswordAsync(basicUser, request.NewPassword);
        await db.SaveChangesAsync();
        logger.LogInformation("重設密碼完成：{Username}", basicUser.Username);
    }

    public async Task<CurrentBasicUserResult> GetCurrentBasicUserAsync(int basicUserId)
    {
        var basicUser = await db.BasicUser.AsNoTracking().FirstOrDefaultAsync(u => u.Id == basicUserId && u.IsActive)
            ?? throw new BusinessException("登入已逾時，請重新登入", StatusCodes.Status401Unauthorized);
        var basicPermissionNames = await basicPermissionsService.GetBasicPermissionNamesAsync(basicUserId) ?? new HashSet<string>();

        return new CurrentBasicUserResult
        {
            Id = basicUser.Id,
            Username = basicUser.Username,
            DisplayName = basicUser.DisplayName,
            Email = basicUser.Email,
            BasicPermissionNames = basicPermissionNames.Order(StringComparer.Ordinal).ToList(),
        };
    }

    /// <summary>
    /// 以「最後一次錯誤前 N 分鐘內」的錯誤次數判斷是否鎖定，鎖定時間自最後一次錯誤起算；回傳剩餘分鐘數（未鎖定為 0）
    /// </summary>
    private async Task<int> GetLockedMinutesAsync(string username, LoginSettings login)
    {
        var now = DateTime.Now;
        var since = now.AddMinutes(-(login.FailedWindowMinutes + login.LockoutMinutes));
        var recent = await db.BasicLoginErrlog.AsNoTracking()
            .Where(x => x.Username == username && x.CreatedAt >= since)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.CreatedAt)
            .Take(login.MaxFailedAttempts)
            .ToListAsync();
        if (recent.Count < login.MaxFailedAttempts)
        {
            return 0;
        }

        var last = recent[0];
        var first = recent[^1];
        var lockedUntil = last.AddMinutes(login.LockoutMinutes);
        if (last - first > TimeSpan.FromMinutes(login.FailedWindowMinutes) || lockedUntil <= now)
        {
            return 0;
        }

        return (int)Math.Ceiling((lockedUntil - now).TotalMinutes);
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

using System.Security.Cryptography;
using System.Text;
using backend.Data;
using backend.DTOs.Auth;
using backend.Exceptions;
using backend.Models;
using backend.Services.Captcha;
using backend.Services.Mail;
using backend.Services.Password;
using backend.Services.BasicPermissions;
using backend.Services.Token;
using backend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace backend.Services.Auth;

public class AuthService(
    AppDbContext db,
    IPasswordService passwordService,
    ICaptchaService captchaService,
    ITokenService tokenService,
    IBasicPermissionsService basicPermissionsService,
    IMailService mailService,
    IMemoryCache cache,
    IOptions<SecuritySettings> securityOptions,
    IOptions<AppSettings> appOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private const string InvalidCredentialMessage = "帳號或密碼錯誤";
    private const string ChangeTokenCachePrefix = "pwd-change:";

    /// <summary>帳號不存在時用來比對的雜湊，讓回應時間與帳號存在時一致</summary>
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), 12);

    private readonly SecuritySettings _security = securityOptions.Value;
    private readonly AppSettings _app = appOptions.Value;

    public AuthOptionsResult GetOptions() => new()
    {
        SystemName = _app.SystemName,
        CaptchaEnabled = _security.Login.CaptchaEnabled,
        CaptchaLength = _security.Login.CaptchaLength,
        PasswordMinLength = _security.Password.MinLength,
        PasswordRequiredCategories = _security.Password.RequiredCategories,
        PasswordHistoryCount = _security.Password.HistoryCount,
        PasswordExpireDays = _security.Password.ExpireDays,
        ResetTokenExpireMinutes = _security.PasswordReset.TokenExpireMinutes
    };

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        // 驗證碼錯誤不計入錯誤次數
        EnsureCaptcha(request.CaptchaId, request.CaptchaCode);

        var username = request.Username.Trim();
        var now = DateTime.Now;

        // 鎖定期間不驗證密碼，也不再寫入紀錄
        var lockedUntil = await GetLockedUntilAsync(username, now);
        if (lockedUntil != null)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling((lockedUntil.Value - now).TotalMinutes));
            throw new BusinessException($"登入失敗次數過多，請於 {minutes} 分鐘後再試");
        }

        var basicUser = await db.BasicUser.FirstOrDefaultAsync(u => u.Username == username);
        var passwordValid = passwordService.Verify(request.Password, basicUser?.PasswordHash ?? DummyHash);
        if (basicUser == null || !passwordValid)
        {
            // 帳號不存在也記錄
            db.BasicLoginErrlog.Add(new BasicLoginErrlog { Username = username, CreatedAt = now });
            await db.SaveChangesAsync();
            logger.LogWarning("登入失敗（帳號或密碼錯誤）：{Username}", username);
            throw new BusinessException(InvalidCredentialMessage);
        }

        if (!basicUser.IsActive)
        {
            logger.LogWarning("登入失敗（帳號已停用）：{BasicUserId}", basicUser.Id);
            throw new BusinessException("帳號已停用，請聯繫管理員");
        }

        // 密碼逾期時不發 JWT，改發一次性、限時的變更憑證
        if (await passwordService.IsExpiredAsync(basicUser.Username))
        {
            var changeToken = CreateRandomToken();
            cache.Set(ChangeTokenCachePrefix + HashToken(changeToken), basicUser.Id,
                TimeSpan.FromMinutes(_security.Password.ChangeTokenExpireMinutes));
            logger.LogInformation("密碼已逾期，要求變更：{BasicUserId}", basicUser.Id);
            return new LoginResult { RequirePasswordChange = true, ChangeToken = changeToken };
        }

        var (token, expiresAt) = tokenService.CreateAccessToken(basicUser);
        logger.LogInformation("登入成功：{BasicUserId}", basicUser.Id);

        return new LoginResult
        {
            Token = token,
            ExpiresAt = expiresAt,
            BasicUser = await GetCurrentBasicUserAsync(basicUser.Id)
        };
    }

    public async Task ChangeExpiredPasswordAsync(ChangeExpiredPasswordRequest request)
    {
        var cacheKey = ChangeTokenCachePrefix + HashToken(request.ChangeToken);
        if (!cache.TryGetValue(cacheKey, out int basicUserId))
        {
            throw new BusinessException("變更憑證無效或已過期，請重新登入");
        }

        var basicUser = await db.BasicUser.FirstOrDefaultAsync(u => u.Id == basicUserId && u.IsActive)
            ?? throw new BusinessException("變更憑證無效或已過期，請重新登入");

        // 新密碼不符規則時憑證不消耗
        await passwordService.SetPasswordAsync(basicUser, request.NewPassword);
        await db.SaveChangesAsync();

        cache.Remove(cacheKey);
        logger.LogInformation("逾期密碼已變更：{BasicUserId}", basicUser.Id);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        EnsureCaptcha(request.CaptchaId, request.CaptchaCode);

        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var now = DateTime.Now;
        var settings = _security.PasswordReset;

        var basicUser = await db.BasicUser.FirstOrDefaultAsync(u => u.Username == username && u.Email == email && u.IsActive);

        // 不論帳號與 Email 是否相符都回相同訊息；申請後不變更或停用原密碼
        if (basicUser == null)
        {
            logger.LogInformation("忘記密碼申請（帳號與 Email 不符）");
            return;
        }

        // 限制同帳號申請頻率
        if (basicUser.PasswordResetExpiresAt != null)
        {
            var lastRequestedAt = basicUser.PasswordResetExpiresAt.Value.AddMinutes(-settings.TokenExpireMinutes);
            if (lastRequestedAt.AddMinutes(settings.RequestIntervalMinutes) > now)
            {
                logger.LogInformation("忘記密碼申請過於頻繁，略過：{BasicUserId}", basicUser.Id);
                return;
            }
        }

        // 重新申請即覆蓋，舊連結隨即失效；資料庫只存雜湊
        var token = CreateRandomToken();
        basicUser.PasswordResetTokenHash = HashToken(token);
        basicUser.PasswordResetExpiresAt = now.AddMinutes(settings.TokenExpireMinutes);
        await db.SaveChangesAsync();

        // 連結網址取自設定檔，不以請求的 Host 標頭組成
        var resetLink = $"{_app.FrontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
        mailService.QueueResetPasswordMail(basicUser.Email, basicUser.DisplayName, resetLink, settings.TokenExpireMinutes);
        logger.LogInformation("已排入重設密碼郵件：{BasicUserId}", basicUser.Id);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var tokenHash = HashToken(request.Token);
        var now = DateTime.Now;

        var basicUser = await db.BasicUser.FirstOrDefaultAsync(u =>
            u.PasswordResetTokenHash == tokenHash && u.PasswordResetExpiresAt > now && u.IsActive)
            ?? throw new BusinessException("重設連結無效或已過期，請重新申請");

        // 新密碼不符規則時連結不消耗；成功後由 SetPasswordAsync 作廢連結
        await passwordService.SetPasswordAsync(basicUser, request.NewPassword);
        await db.SaveChangesAsync();
        logger.LogInformation("已透過重設連結變更密碼：{BasicUserId}", basicUser.Id);
    }

    public async Task<CurrentBasicUserResult> GetCurrentBasicUserAsync(int basicUserId)
    {
        var basicUser = await db.BasicUser.AsNoTracking().FirstOrDefaultAsync(u => u.Id == basicUserId && u.IsActive)
            ?? throw new BusinessException("尚未登入或登入已逾時，請重新登入", StatusCodes.Status401Unauthorized);

        var basicPermissions = await basicPermissionsService.GetBasicUserPermissionsAsync(basicUserId);
        return new CurrentBasicUserResult
        {
            Id = basicUser.Id,
            Username = basicUser.Username,
            DisplayName = basicUser.DisplayName,
            Email = basicUser.Email,
            BasicPermissionNames = basicPermissions.OrderBy(p => p).ToList()
        };
    }

    private void EnsureCaptcha(string? captchaId, string? captchaCode)
    {
        if (!_security.Login.CaptchaEnabled) return;
        if (!captchaService.Validate(captchaId, captchaCode))
        {
            throw new BusinessException("驗證碼錯誤或已過期，請重新輸入");
        }
    }

    /// <summary>
    /// 以最近 N 分鐘內的錯誤次數判斷是否鎖定；鎖定至觸發鎖定的最後一次錯誤加上鎖定時間
    /// </summary>
    private async Task<DateTime?> GetLockedUntilAsync(string username, DateTime now)
    {
        var login = _security.Login;
        var since = now.AddMinutes(-(login.FailedWindowMinutes + login.LockoutMinutes));

        var failures = await db.BasicLoginErrlog
            .Where(l => l.Username == username && l.CreatedAt >= since)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => l.CreatedAt)
            .ToListAsync();

        if (failures.Count < login.MaxFailedAttempts) return null;

        var last = failures[0];
        var lockedUntil = last.AddMinutes(login.LockoutMinutes);
        if (lockedUntil <= now) return null;

        var windowStart = last.AddMinutes(-login.FailedWindowMinutes);
        var countInWindow = failures.Count(f => f >= windowStart);
        return countInWindow >= login.MaxFailedAttempts ? lockedUntil : null;
    }

    /// <summary>256 位元隨機 token（URL 安全）</summary>
    private static string CreateRandomToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

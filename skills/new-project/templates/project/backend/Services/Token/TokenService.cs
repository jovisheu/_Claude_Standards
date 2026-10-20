using System.Security.Claims;
using System.Security.Cryptography;
using backend.Models;
using backend.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services.Token;

/// <summary>Singleton；JWT 金鑰為 Base64 字串（解密後）</summary>
public class TokenService(IOptions<JwtSettings> jwtSettings, IOptions<SecuritySettings> securitySettings, IMemoryCache cache) : ITokenService
{
    private const string ChangeTokenCachePrefix = "pwd-change:";

    /// <summary>安全戳記的 Claim 名稱：由密碼雜湊推導，密碼變更後先前發出的 JWT 即失效</summary>
    public const string SecurityStampClaim = "sst";
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTime ExpiresAt) CreateAccessToken(BasicUser basicUser)
    {
        var settings = jwtSettings.Value;
        var now = DateTime.Now;
        var expiresAt = now.AddMinutes(settings.ExpireMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, basicUser.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, basicUser.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(SecurityStampClaim, ComputeSecurityStamp(basicUser.PasswordHash)),
            ]),
            SigningCredentials = new SigningCredentials(CreateSigningKey(settings.Key), SecurityAlgorithms.HmacSha256),
        };

        // 回傳前端的到期時間去除毫秒，與 JWT exp（秒）一致
        return (_handler.CreateToken(descriptor), expiresAt.AddTicks(-(expiresAt.Ticks % TimeSpan.TicksPerSecond)));
    }

    public string CreatePasswordChangeToken(int basicUserId)
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var minutes = securitySettings.Value.Password.ChangeTokenExpireMinutes;
        cache.Set(ChangeTokenCachePrefix + Hash(token), basicUserId, TimeSpan.FromMinutes(minutes));
        return token;
    }

    public int? GetPasswordChangeBasicUserId(string token)
        => cache.TryGetValue(ChangeTokenCachePrefix + Hash(token), out int basicUserId) ? basicUserId : null;

    public void RevokePasswordChangeToken(string token) => cache.Remove(ChangeTokenCachePrefix + Hash(token));

    /// <summary>以 Base64 解碼簽章金鑰（須與產生金鑰時的格式一致）</summary>
    public static SymmetricSecurityKey CreateSigningKey(string base64Key) => new(Convert.FromBase64String(base64Key));

    /// <summary>JWT 的安全戳記是否與目前的密碼雜湊相符（固定時間比對）</summary>
    public static bool IsSecurityStampValid(string passwordHash, string? securityStamp)
    {
        return securityStamp is not null
            && CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(ComputeSecurityStamp(passwordHash)),
                System.Text.Encoding.ASCII.GetBytes(securityStamp));
    }

    /// <summary>由密碼雜湊推導安全戳記（SHA-256 前 16 位元組），不放入雜湊本身</summary>
    private static string ComputeSecurityStamp(string passwordHash)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(passwordHash)), 0, 16);

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}

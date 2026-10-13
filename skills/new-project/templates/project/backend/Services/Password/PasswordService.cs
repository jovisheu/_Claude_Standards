using backend.Data;
using backend.Exceptions;
using backend.Models;
using backend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services.Password;

public class PasswordService(AppDbContext db, IOptions<SecuritySettings> securityOptions, ILogger<PasswordService> logger) : IPasswordService
{
    private const int WorkFactor = 12;
    private readonly PasswordSettings _settings = securityOptions.Value.Password;

    public async Task SetPasswordAsync(User user, string newPassword)
    {
        ValidateRules(newPassword);

        // 不得與前 N 次密碼相同
        if (_settings.HistoryCount > 0 && !string.IsNullOrEmpty(user.Username))
        {
            var recentHashes = await db.PwdLogs
                .Where(p => p.Username == user.Username)
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Take(_settings.HistoryCount)
                .Select(p => p.PasswordHash)
                .ToListAsync();

            if (recentHashes.Any(h => Verify(newPassword, h)))
            {
                throw new BusinessException($"新密碼不得與前 {_settings.HistoryCount} 次使用的密碼相同");
            }
        }

        var now = DateTime.Now;
        var hash = BCrypt.Net.BCrypt.HashPassword(newPassword, WorkFactor);

        user.PasswordHash = hash;
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresAt = null;
        if (user.Id != 0) user.UpdatedAt = now;

        db.PwdLogs.Add(new PwdLog { Username = user.Username, PasswordHash = hash, CreatedAt = now });

        logger.LogInformation("密碼已設定：{Username}", user.Username);
    }

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    public async Task<bool> IsExpiredAsync(string username)
    {
        var lastChanged = await db.PwdLogs
            .Where(p => p.Username == username)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => (DateTime?)p.CreatedAt)
            .FirstOrDefaultAsync();

        return lastChanged == null || lastChanged.Value.AddDays(_settings.ExpireDays) <= DateTime.Now;
    }

    /// <summary>
    /// 密碼規則：至少 N 個半形可列印字元（0x21–0x7E，不含空白）；大寫、小寫、數字、特殊符號至少符合 M 類
    /// </summary>
    private void ValidateRules(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < _settings.MinLength)
        {
            throw new BusinessException($"密碼至少須 {_settings.MinLength} 個字元");
        }

        if (password.Any(c => c < 0x21 || c > 0x7E))
        {
            throw new BusinessException("密碼只能使用半形英數字與符號，且不可包含空白");
        }

        var categories = 0;
        if (password.Any(char.IsAsciiLetterUpper)) categories++;
        if (password.Any(char.IsAsciiLetterLower)) categories++;
        if (password.Any(char.IsAsciiDigit)) categories++;
        if (password.Any(c => !char.IsAsciiLetterOrDigit(c))) categories++;

        if (categories < _settings.RequiredCategories)
        {
            throw new BusinessException($"密碼須包含大寫字母、小寫字母、數字、特殊符號其中 {_settings.RequiredCategories} 種");
        }
    }
}

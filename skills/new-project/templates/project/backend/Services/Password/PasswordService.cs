using backend.Data;
using backend.Exceptions;
using backend.Models;
using backend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services.Password;

public class PasswordService(AppDbContext db, IOptions<SecuritySettings> settings) : IPasswordService
{
    private const int WorkFactor = 12;
    // BCrypt 只取前 72 位元組，上限須小於此值，超過的部分才不會被忽略
    private const int MaxLength = 64;

    public async Task SetPasswordAsync(BasicUser basicUser, string newPassword)
    {
        var passwordSettings = settings.Value.Password;
        var errors = ValidateRules(newPassword, passwordSettings);
        if (errors.Count > 0)
        {
            throw new BusinessException(string.Join("\n", errors));
        }

        // 不得與前 N 次相同（新使用者尚未有歷程）
        if (!string.IsNullOrEmpty(basicUser.Username) && passwordSettings.HistoryCount > 0)
        {
            var recentHashes = await db.BasicPwdlog.AsNoTracking()
                .Where(x => x.Username == basicUser.Username)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(passwordSettings.HistoryCount)
                .Select(x => x.PasswordHash)
                .ToListAsync();
            if (recentHashes.Any(hash => Verify(newPassword, hash)))
            {
                throw new BusinessException($"新密碼不得與前 {passwordSettings.HistoryCount} 次使用的密碼相同");
            }
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, WorkFactor);
        var now = DateTime.Now;
        basicUser.PasswordHash = passwordHash;
        basicUser.PasswordResetTokenHash = null;
        basicUser.PasswordResetExpiresAt = null;
        db.BasicPwdlog.Add(new BasicPwdlog { Username = basicUser.Username, PasswordHash = passwordHash, CreatedAt = now });
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
        var lastChangedAt = await db.BasicPwdlog.AsNoTracking()
            .Where(x => x.Username == username)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (DateTime?)x.CreatedAt)
            .FirstOrDefaultAsync();
        return lastChangedAt is null || lastChangedAt.Value.AddDays(settings.Value.Password.ExpireDays) <= DateTime.Now;
    }

    /// <summary>密碼規則：只接受 0x21–0x7E；至少 N 字元；大寫、小寫、數字、特殊符號至少符合 M 種</summary>
    private static List<string> ValidateRules(string password, PasswordSettings passwordSettings)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password))
        {
            errors.Add("請輸入密碼");
            return errors;
        }

        if (password.Any(c => c < 0x21 || c > 0x7E))
        {
            errors.Add("密碼只能使用半形英文、數字與符號（不含空白）");
        }

        if (password.Length < passwordSettings.MinLength)
        {
            errors.Add($"密碼至少須 {passwordSettings.MinLength} 個字元");
        }

        if (password.Length > MaxLength)
        {
            errors.Add($"密碼不可超過 {MaxLength} 個字元");
        }

        var categories = 0;
        if (password.Any(char.IsAsciiLetterUpper)) categories++;
        if (password.Any(char.IsAsciiLetterLower)) categories++;
        if (password.Any(char.IsAsciiDigit)) categories++;
        if (password.Any(c => c is >= (char)0x21 and <= (char)0x7E && !char.IsAsciiLetterOrDigit(c))) categories++;
        if (categories < passwordSettings.RequiredCategoryCount)
        {
            errors.Add($"密碼須包含大寫字母、小寫字母、數字、特殊符號其中 {passwordSettings.RequiredCategoryCount} 種");
        }

        return errors;
    }
}

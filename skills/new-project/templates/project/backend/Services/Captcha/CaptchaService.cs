using System.Security.Cryptography;
using System.Text;
using backend.DTOs.Auth;
using backend.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace backend.Services.Captcha;

/// <summary>
/// 圖形驗證碼（Singleton）：答案只存於伺服器記憶體，限時且一次性；以 SkiaSharp 繪製（隨機旋轉、干擾線與雜點），
/// 亂數一律使用 RandomNumberGenerator
/// </summary>
public class CaptchaService(IMemoryCache cache, IOptions<SecuritySettings> settings) : ICaptchaService
{
    private const string CachePrefix = "captcha:";

    // 排除易混淆的 I、O、0、1
    private const string Characters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private const int Width = 150;
    private const int Height = 48;

    public CaptchaResult Create()
    {
        var login = settings.Value.Login;
        var length = Math.Clamp(login.CaptchaLength, 4, 8);
        var code = RandomNumberGenerator.GetString(Characters, length);
        var captchaId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var expire = TimeSpan.FromMinutes(login.CaptchaExpireMinutes);
        cache.Set(CachePrefix + captchaId, code, expire);

        return new CaptchaResult
        {
            CaptchaId = captchaId,
            Image = "data:image/png;base64," + Convert.ToBase64String(Render(code)),
            ExpireSeconds = (int)expire.TotalSeconds,
        };
    }

    public bool Validate(string? captchaId, string? code)
    {
        if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var key = CachePrefix + captchaId;
        if (!cache.TryGetValue(key, out string? expected) || expected is null)
        {
            return false;
        }

        // 一次性使用：比對前即作廢
        cache.Remove(key);
        var actual = Encoding.ASCII.GetBytes(code.Trim().ToUpperInvariant());
        return CryptographicOperations.FixedTimeEquals(actual, Encoding.ASCII.GetBytes(expected));
    }

    private static byte[] Render(string code)
    {
        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(245, 247, 250));

        // 伺服器沒有 Arial 時退回預設字型
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default;
        using var font = new SKFont(typeface, 30);
        using var paint = new SKPaint { IsAntialias = true };

        // 干擾線
        for (var i = 0; i < 6; i++)
        {
            paint.Color = RandomColor(120, 200);
            paint.StrokeWidth = 1 + RandomNumberGenerator.GetInt32(2);
            canvas.DrawLine(RandomNumberGenerator.GetInt32(Width), RandomNumberGenerator.GetInt32(Height),
                RandomNumberGenerator.GetInt32(Width), RandomNumberGenerator.GetInt32(Height), paint);
        }

        // 字元（隨機旋轉與位移）
        var step = (Width - 20f) / code.Length;
        for (var i = 0; i < code.Length; i++)
        {
            paint.Color = RandomColor(20, 110);
            canvas.Save();
            canvas.Translate(12 + i * step + RandomNumberGenerator.GetInt32(4), 34 + RandomNumberGenerator.GetInt32(6));
            canvas.RotateDegrees(RandomNumberGenerator.GetInt32(-25, 26));
            canvas.DrawText(code[i].ToString(), 0, 0, SKTextAlign.Left, font, paint);
            canvas.Restore();
        }

        // 雜點
        for (var i = 0; i < 120; i++)
        {
            paint.Color = RandomColor(80, 220);
            canvas.DrawPoint(RandomNumberGenerator.GetInt32(Width), RandomNumberGenerator.GetInt32(Height), paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static SKColor RandomColor(int min, int max)
        => new((byte)RandomNumberGenerator.GetInt32(min, max),
               (byte)RandomNumberGenerator.GetInt32(min, max),
               (byte)RandomNumberGenerator.GetInt32(min, max));
}

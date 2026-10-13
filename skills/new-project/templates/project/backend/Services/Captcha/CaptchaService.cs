using System.Security.Cryptography;
using System.Text;
using backend.DTOs.Auth;
using backend.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace backend.Services.Captcha;

public class CaptchaService(IMemoryCache cache, IOptions<SecuritySettings> securityOptions) : ICaptchaService
{
    // 排除易混淆字元（0/O、1/I/L）
    private const string Characters = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const string CacheKeyPrefix = "captcha:";
    private const int Width = 150;
    private const int Height = 48;

    private readonly LoginSettings _settings = securityOptions.Value.Login;

    public CaptchaResult Create()
    {
        var length = Math.Clamp(_settings.CaptchaLength, 4, 8);
        var code = new string(Enumerable.Range(0, length)
            .Select(_ => Characters[RandomNumberGenerator.GetInt32(Characters.Length)])
            .ToArray());
        var captchaId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        cache.Set(CacheKeyPrefix + captchaId, code, TimeSpan.FromMinutes(_settings.CaptchaExpireMinutes));

        return new CaptchaResult
        {
            CaptchaId = captchaId,
            Image = "data:image/png;base64," + Convert.ToBase64String(Render(code))
        };
    }

    public bool Validate(string? captchaId, string? code)
    {
        if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(code)) return false;

        var key = CacheKeyPrefix + captchaId;
        if (!cache.TryGetValue(key, out string? expected) || expected == null) return false;

        // 一次性使用
        cache.Remove(key);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(code.Trim().ToUpperInvariant()));
    }

    private static byte[] Render(string code)
    {
        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(245, 247, 250));

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

        // 干擾點
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

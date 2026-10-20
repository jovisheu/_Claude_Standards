using backend.DTOs.Auth;

namespace backend.Services.Captcha;

public interface ICaptchaService
{
    /// <summary>產生驗證碼圖片（後端產生與保存答案，限時）</summary>
    CaptchaResult Create();

    /// <summary>比對驗證碼；不論結果皆作廢（一次性使用）</summary>
    bool Validate(string? captchaId, string? code);
}

using backend.DTOs.Auth;

namespace backend.Services.Captcha;

public interface ICaptchaService
{
    /// <summary>產生驗證碼圖片（後端保存答案，限時）</summary>
    CaptchaResult Create();

    /// <summary>比對驗證碼；不論成功與否皆作廢（一次性）</summary>
    bool Validate(string? captchaId, string? code);
}

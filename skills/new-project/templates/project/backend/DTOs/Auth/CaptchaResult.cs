namespace backend.DTOs.Auth;

public class CaptchaResult
{
    public string CaptchaId { get; set; } = string.Empty;

    /// <summary>PNG 圖片（data URL）</summary>
    public string Image { get; set; } = string.Empty;

    public int ExpireSeconds { get; set; }
}

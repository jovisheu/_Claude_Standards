using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "請輸入帳號")]
    [MaxLength(50, ErrorMessage = "帳號最多 50 個字元")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入 Email")]
    [MaxLength(256, ErrorMessage = "Email 最多 256 個字元")]
    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(64, ErrorMessage = "驗證碼錯誤")]
    public string? CaptchaId { get; set; }

    [MaxLength(10, ErrorMessage = "驗證碼錯誤")]
    public string? CaptchaCode { get; set; }
}

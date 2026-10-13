using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "請輸入帳號")]
    [MaxLength(50, ErrorMessage = "帳號或密碼錯誤")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [MaxLength(64, ErrorMessage = "帳號或密碼錯誤")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(64, ErrorMessage = "驗證碼錯誤")]
    public string? CaptchaId { get; set; }

    [MaxLength(10, ErrorMessage = "驗證碼錯誤")]
    public string? CaptchaCode { get; set; }
}

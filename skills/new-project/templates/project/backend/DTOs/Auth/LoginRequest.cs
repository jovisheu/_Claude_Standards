using System.ComponentModel.DataAnnotations;
using backend.Extensions;

namespace backend.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "請輸入帳號")]
    [StringLength(50, ErrorMessage = "帳號不可超過 50 字")]
    [NoHtml(ErrorMessage = "帳號不可包含 HTML 標籤")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [StringLength(64, ErrorMessage = "密碼不可超過 64 字")]
    public string Password { get; set; } = string.Empty;

    [StringLength(64, ErrorMessage = "驗證碼格式不正確")]
    public string? CaptchaId { get; set; }

    [StringLength(10, ErrorMessage = "驗證碼格式不正確")]
    public string? CaptchaCode { get; set; }
}

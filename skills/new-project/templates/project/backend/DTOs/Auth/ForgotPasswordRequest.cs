using System.ComponentModel.DataAnnotations;
using backend.Extensions;

namespace backend.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "請輸入帳號")]
    [StringLength(50, ErrorMessage = "帳號不可超過 50 字")]
    [NoHtml(ErrorMessage = "帳號不可包含 HTML 標籤")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入 Email")]
    [StringLength(256, ErrorMessage = "Email 不可超過 256 字")]
    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    [NoHtml(ErrorMessage = "Email 不可包含 HTML 標籤")]
    public string Email { get; set; } = string.Empty;

    [StringLength(64, ErrorMessage = "驗證碼格式不正確")]
    public string? CaptchaId { get; set; }

    [StringLength(10, ErrorMessage = "驗證碼格式不正確")]
    public string? CaptchaCode { get; set; }
}

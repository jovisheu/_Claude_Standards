using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "重設連結無效或已過期")]
    [StringLength(100, ErrorMessage = "重設連結無效或已過期")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入新密碼")]
    [StringLength(64, ErrorMessage = "密碼不可超過 64 字")]
    public string NewPassword { get; set; } = string.Empty;
}

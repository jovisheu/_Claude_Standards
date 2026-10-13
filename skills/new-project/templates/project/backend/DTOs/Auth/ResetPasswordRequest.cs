using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "重設連結無效或已過期")]
    [MaxLength(128, ErrorMessage = "重設連結無效或已過期")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入新密碼")]
    [MaxLength(64, ErrorMessage = "密碼最多 64 個字元")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "請再次輸入新密碼")]
    [Compare(nameof(NewPassword), ErrorMessage = "兩次輸入的密碼不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

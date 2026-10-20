using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Auth;

public class ChangeExpiredPasswordRequest
{
    [Required(ErrorMessage = "變更憑證無效，請重新登入")]
    [StringLength(100, ErrorMessage = "變更憑證無效，請重新登入")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入新密碼")]
    [StringLength(64, ErrorMessage = "密碼不可超過 64 字")]
    public string NewPassword { get; set; } = string.Empty;
}

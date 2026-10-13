using System.ComponentModel.DataAnnotations;
using backend.Extensions;

namespace backend.DTOs.Users;

public class CreateUserRequest
{
    [Required(ErrorMessage = "請輸入帳號")]
    [RegularExpression(@"^[A-Za-z0-9_.\-]{3,50}$", ErrorMessage = "帳號須為 3～50 個英數字或 _ . -")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入 Email")]
    [MaxLength(256, ErrorMessage = "Email 最多 256 個字元")]
    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    [NoHtml(ErrorMessage = "Email 格式不正確")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入顯示名稱")]
    [MaxLength(50, ErrorMessage = "顯示名稱最多 50 個字")]
    [NoHtml(ErrorMessage = "顯示名稱不可包含 HTML 標籤")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [MaxLength(64, ErrorMessage = "密碼最多 64 個字元")]
    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<int> GroupIds { get; set; } = [];
}

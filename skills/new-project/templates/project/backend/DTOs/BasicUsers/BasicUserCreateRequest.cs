using System.ComponentModel.DataAnnotations;
using backend.Extensions;

namespace backend.DTOs.BasicUsers;

public class BasicUserCreateRequest
{
    [Required(ErrorMessage = "請輸入帳號")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "帳號須為 3 到 50 字")]
    [RegularExpression(@"^[A-Za-z0-9._@-]+$", ErrorMessage = "帳號只能使用英文、數字與 . _ @ -")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入 Email")]
    [StringLength(256, ErrorMessage = "Email 不可超過 256 字")]
    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    [NoHtml(ErrorMessage = "Email 不可包含 HTML 標籤")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入顯示名稱")]
    [StringLength(50, ErrorMessage = "顯示名稱不可超過 50 字")]
    [NoHtml(ErrorMessage = "顯示名稱不可包含 HTML 標籤")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [StringLength(64, ErrorMessage = "密碼不可超過 64 字")]
    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [MaxLength(100, ErrorMessage = "群組數量過多")]
    public List<int> BasicGroupIds { get; set; } = [];
}

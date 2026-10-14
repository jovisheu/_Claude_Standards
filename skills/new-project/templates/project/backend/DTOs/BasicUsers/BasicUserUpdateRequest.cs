using System.ComponentModel.DataAnnotations;
using backend.Extensions;

namespace backend.DTOs.BasicUsers;

public class BasicUserUpdateRequest
{
    [Required(ErrorMessage = "請輸入 Email")]
    [MaxLength(256, ErrorMessage = "Email 最多 256 個字元")]
    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    [NoHtml(ErrorMessage = "Email 格式不正確")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入顯示名稱")]
    [MaxLength(50, ErrorMessage = "顯示名稱最多 50 個字")]
    [NoHtml(ErrorMessage = "顯示名稱不可包含 HTML 標籤")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>留白表示不變更密碼</summary>
    [MaxLength(64, ErrorMessage = "密碼最多 64 個字元")]
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;

    public List<int> BasicGroupIds { get; set; } = [];
}

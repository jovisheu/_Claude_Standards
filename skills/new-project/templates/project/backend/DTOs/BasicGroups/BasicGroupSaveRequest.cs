using System.ComponentModel.DataAnnotations;
using backend.Extensions;

namespace backend.DTOs.BasicGroups;

/// <summary>
/// 新增／編輯群組
/// </summary>
public class BasicGroupSaveRequest
{
    [Required(ErrorMessage = "請輸入群組名稱")]
    [MaxLength(50, ErrorMessage = "群組名稱最多 50 個字")]
    [NoHtml(ErrorMessage = "群組名稱不可包含 HTML 標籤")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "說明最多 200 個字")]
    [NoHtml(ErrorMessage = "說明不可包含 HTML 標籤")]
    public string? Description { get; set; }

    public List<int> BasicPermissionIds { get; set; } = [];
}

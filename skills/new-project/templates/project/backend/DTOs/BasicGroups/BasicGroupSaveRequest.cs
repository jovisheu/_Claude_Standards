using System.ComponentModel.DataAnnotations;
using backend.Extensions;

namespace backend.DTOs.BasicGroups;

/// <summary>新增、編輯群組</summary>
public class BasicGroupSaveRequest
{
    [Required(ErrorMessage = "請輸入群組名稱")]
    [StringLength(50, ErrorMessage = "群組名稱不可超過 50 字")]
    [NoHtml(ErrorMessage = "群組名稱不可包含 HTML 標籤")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "群組說明不可超過 200 字")]
    [NoHtml(ErrorMessage = "群組說明不可包含 HTML 標籤")]
    public string? Description { get; set; }

    [MaxLength(500, ErrorMessage = "權限數量過多")]
    public List<int> BasicPermissionIds { get; set; } = [];
}

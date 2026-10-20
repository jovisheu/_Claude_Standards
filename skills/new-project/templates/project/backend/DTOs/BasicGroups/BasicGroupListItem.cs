namespace backend.DTOs.BasicGroups;

public class BasicGroupListItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>受保護群組（系統管理員）：不可刪除、權限不可修改</summary>
    public bool IsProtected { get; set; }

    /// <summary>操作者是否可管理此群組（群組權限未超過操作者）</summary>
    public bool IsManageable { get; set; }

    public int BasicUserCount { get; set; }

    public int BasicPermissionCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

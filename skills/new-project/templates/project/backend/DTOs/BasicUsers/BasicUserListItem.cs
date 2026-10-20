namespace backend.DTOs.BasicUsers;

public class BasicUserListItem
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<string> BasicGroupNames { get; set; } = [];

    /// <summary>操作者是否可管理此使用者（對方權限未超過操作者）</summary>
    public bool IsManageable { get; set; }

    /// <summary>是否為操作者本人（不可停用、刪除自己）</summary>
    public bool IsSelf { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

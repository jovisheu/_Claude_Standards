namespace backend.Models;

/// <summary>
/// 群組（角色）
/// </summary>
public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    public ICollection<GroupPermission> GroupPermissions { get; set; } = new List<GroupPermission>();
}

namespace backend.Models;

/// <summary>
/// 群組（角色）
/// </summary>
public class BasicGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<BasicUserGroup> BasicUserGroup { get; set; } = new List<BasicUserGroup>();
    public ICollection<BasicGroupPermission> BasicGroupPermission { get; set; } = new List<BasicGroupPermission>();
}

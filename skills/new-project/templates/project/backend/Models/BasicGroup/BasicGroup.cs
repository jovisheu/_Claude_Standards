namespace backend.Models;

/// <summary>群組（角色）</summary>
public class BasicGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<BasicUserGroup> BasicUserGroup { get; set; } = [];

    public ICollection<BasicGroupPermission> BasicGroupPermission { get; set; } = [];
}

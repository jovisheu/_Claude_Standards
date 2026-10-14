namespace backend.DTOs.BasicGroups;

public class BasicGroupListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BasicUserCount { get; set; }
    public int BasicPermissionCount { get; set; }
    public bool IsProtected { get; set; }
    public DateTime CreatedAt { get; set; }
}

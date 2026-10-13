namespace backend.DTOs.Groups;

public class GroupDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsProtected { get; set; }
    public List<int> PermissionIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

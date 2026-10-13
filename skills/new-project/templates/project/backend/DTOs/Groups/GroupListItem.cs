namespace backend.DTOs.Groups;

public class GroupListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MemberCount { get; set; }
    public int PermissionCount { get; set; }
    public bool IsProtected { get; set; }
    public DateTime CreatedAt { get; set; }
}

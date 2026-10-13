namespace backend.DTOs.Users;

public class UserListItem
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> GroupNames { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

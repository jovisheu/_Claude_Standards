namespace backend.DTOs.BasicUsers;

public class BasicUserDetail
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<int> BasicGroupIds { get; set; } = [];

    public bool IsManageable { get; set; }

    public bool IsSelf { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

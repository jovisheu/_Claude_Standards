namespace backend.DTOs.BasicGroups;

public class BasicGroupDetail
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsProtected { get; set; }

    public bool IsManageable { get; set; }

    public List<int> BasicPermissionIds { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

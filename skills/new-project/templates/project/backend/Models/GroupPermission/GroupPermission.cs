namespace backend.Models;

/// <summary>
/// 群組與權限多對多
/// </summary>
public class GroupPermission
{
    public int GroupId { get; set; }
    public int PermissionId { get; set; }

    public Group Group { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

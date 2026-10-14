namespace backend.Models;

/// <summary>
/// 群組與權限多對多
/// </summary>
public class BasicGroupPermission
{
    public int BasicGroupId { get; set; }
    public int BasicPermissionId { get; set; }

    public BasicGroup BasicGroup { get; set; } = null!;
    public BasicPermission BasicPermission { get; set; } = null!;
}

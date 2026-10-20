namespace backend.Models;

/// <summary>群組與權限對應</summary>
public class BasicGroupPermission
{
    public int BasicGroupId { get; set; }

    public int BasicPermissionId { get; set; }

    public BasicGroup? BasicGroup { get; set; }

    public BasicPermission? BasicPermission { get; set; }
}

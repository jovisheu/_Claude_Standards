namespace backend.DTOs.BasicGroups;

public class BasicPermissionItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Module { get; set; } = string.Empty;

    /// <summary>操作者是否擁有此權限（不可授予高於自身的權限）</summary>
    public bool Grantable { get; set; }
}

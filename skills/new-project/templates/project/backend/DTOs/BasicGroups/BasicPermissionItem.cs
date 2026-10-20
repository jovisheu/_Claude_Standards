namespace backend.DTOs.BasicGroups;

/// <summary>群組表單的權限選項</summary>
public class BasicPermissionItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Module { get; set; } = string.Empty;

    /// <summary>操作者可否授予（操作者本身擁有此權限）</summary>
    public bool IsAssignable { get; set; }
}

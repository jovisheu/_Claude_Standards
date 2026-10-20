namespace backend.Models;

/// <summary>權限（以靜態種子建立）</summary>
public class BasicPermission
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Module { get; set; } = string.Empty;

    public ICollection<BasicGroupPermission> BasicGroupPermission { get; set; } = [];
}

namespace backend.Models;

/// <summary>
/// 使用者與群組多對多
/// </summary>
public class BasicUserGroup
{
    public int BasicUserId { get; set; }
    public int BasicGroupId { get; set; }

    public BasicUser BasicUser { get; set; } = null!;
    public BasicGroup BasicGroup { get; set; } = null!;
}

namespace backend.Models;

/// <summary>使用者與群組對應</summary>
public class BasicUserGroup
{
    public int BasicUserId { get; set; }

    public int BasicGroupId { get; set; }

    public BasicUser? BasicUser { get; set; }

    public BasicGroup? BasicGroup { get; set; }
}

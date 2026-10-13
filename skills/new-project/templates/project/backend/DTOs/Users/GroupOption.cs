namespace backend.DTOs.Users;

/// <summary>
/// 使用者表單的群組選項
/// </summary>
public class GroupOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>操作者是否可指派此群組（不可授予高於自身的權限）</summary>
    public bool Assignable { get; set; }
}

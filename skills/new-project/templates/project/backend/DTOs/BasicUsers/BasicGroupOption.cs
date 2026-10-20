namespace backend.DTOs.BasicUsers;

/// <summary>使用者表單的群組選項</summary>
public class BasicGroupOption
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>操作者可否指派（群組權限未超過操作者）</summary>
    public bool IsAssignable { get; set; }
}

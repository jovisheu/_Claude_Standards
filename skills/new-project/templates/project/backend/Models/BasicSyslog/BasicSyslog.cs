namespace backend.Models;

/// <summary>系統異動歷程</summary>
public class BasicSyslog
{
    public long Id { get; set; }

    /// <summary>C 新增、U 修改、D 刪除</summary>
    public string Action { get; set; } = string.Empty;

    public string MethodName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string BeforeData { get; set; } = "[]";

    public string AfterData { get; set; } = "[]";

    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; }
}

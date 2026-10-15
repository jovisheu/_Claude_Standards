namespace backend.DTOs.BasicSyslogs;

/// <summary>
/// 異動歷程中的欄位值
/// </summary>
public class BasicSyslogHistoryField
{
    /// <summary>欄位名稱</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>欄位描述（MS_Description）</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>異動前的值（新增時為 null）</summary>
    public string? Before { get; set; }

    /// <summary>異動後的值（刪除時為 null）</summary>
    public string? After { get; set; }
}

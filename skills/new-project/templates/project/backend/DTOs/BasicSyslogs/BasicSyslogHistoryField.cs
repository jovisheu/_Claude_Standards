namespace backend.DTOs.BasicSyslogs;

/// <summary>欄位異動（只列出有變更的欄位；新增、刪除列出全部欄位）</summary>
public class BasicSyslogHistoryField
{
    public string Name { get; set; } = string.Empty;

    /// <summary>欄位描述（無描述時同 Name）</summary>
    public string Label { get; set; } = string.Empty;

    public string? Before { get; set; }

    public string? After { get; set; }
}

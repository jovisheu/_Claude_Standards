namespace backend.DTOs.BasicSyslogs;

/// <summary>一個資料列的異動（主表或關聯表）</summary>
public class BasicSyslogHistoryRow
{
    public string Table { get; set; } = string.Empty;

    /// <summary>資料表描述（無描述時同 Table）</summary>
    public string TableLabel { get; set; } = string.Empty;

    /// <summary>C 新增、U 修改、D 刪除</summary>
    public string Action { get; set; } = string.Empty;

    public List<BasicSyslogHistoryField> Fields { get; set; } = [];
}

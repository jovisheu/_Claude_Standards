namespace backend.DTOs.BasicSyslogs;

/// <summary>
/// 異動歷程中的一列資料
/// </summary>
public class BasicSyslogHistoryRow
{
    /// <summary>資料表名稱</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>資料表描述（MS_Description）</summary>
    public string TableDescription { get; set; } = string.Empty;

    /// <summary>此列的類別：C 新增、U 修改、D 刪除</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>欄位值；修改時只列出有變動的欄位</summary>
    public List<BasicSyslogHistoryField> Fields { get; set; } = [];
}

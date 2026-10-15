namespace backend.DTOs.BasicSyslogs;

/// <summary>
/// 單筆資料的異動歷程查詢結果
/// </summary>
public class BasicSyslogHistoryResult
{
    /// <summary>資料庫保留天數（BasicSyslog:RetentionDays），較舊的紀錄已轉存至伺服器檔案</summary>
    public int RetentionDays { get; set; }

    /// <summary>異動歷程（新到舊）</summary>
    public List<BasicSyslogHistoryItem> Items { get; set; } = [];
}

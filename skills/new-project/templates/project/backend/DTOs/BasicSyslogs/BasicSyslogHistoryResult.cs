namespace backend.DTOs.BasicSyslogs;

/// <summary>單筆資料的異動歷程</summary>
public class BasicSyslogHistoryResult
{
    /// <summary>資料庫保留天數（前端提示用）</summary>
    public int RetentionDays { get; set; }

    public List<BasicSyslogHistoryItem> Items { get; set; } = [];
}

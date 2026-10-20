namespace backend.DTOs.BasicSyslogs;

/// <summary>一次異動（一筆 BasicSyslog）</summary>
public class BasicSyslogHistoryItem
{
    public long Id { get; set; }

    /// <summary>C 新增、U 修改、D 刪除</summary>
    public string Action { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>與該筆資料相關的資料列異動</summary>
    public List<BasicSyslogHistoryRow> Rows { get; set; } = [];
}

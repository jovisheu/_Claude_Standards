namespace backend.DTOs.BasicSyslogs;

/// <summary>
/// 單筆資料的一次異動歷程
/// </summary>
public class BasicSyslogHistoryItem
{
    public long Id { get; set; }

    /// <summary>類別：C 新增、U 修改、D 刪除</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>業務說明（選填，如審核通過）</summary>
    public string? Description { get; set; }

    /// <summary>操作者帳號；未登入（如重設密碼）為 null</summary>
    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>與該筆資料相關的異動（主表與指定的關聯表）</summary>
    public List<BasicSyslogHistoryRow> Rows { get; set; } = [];
}

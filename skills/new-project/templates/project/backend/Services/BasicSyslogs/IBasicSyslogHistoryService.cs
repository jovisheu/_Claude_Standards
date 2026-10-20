using backend.DTOs.BasicSyslogs;

namespace backend.Services.BasicSyslogs;

public interface IBasicSyslogHistoryService
{
    /// <summary>
    /// 查詢單筆資料於保留天數內的異動歷程：主表 Id 相符，或關聯表的 {主表}Id 相符
    /// </summary>
    Task<BasicSyslogHistoryResult> GetRecordHistoryAsync(string table, int id, params string[] relatedTables);
}

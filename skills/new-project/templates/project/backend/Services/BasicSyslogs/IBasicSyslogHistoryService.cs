using backend.DTOs.BasicSyslogs;

namespace backend.Services.BasicSyslogs;

public interface IBasicSyslogHistoryService
{
    /// <summary>
    /// 查詢單筆資料的異動歷程（新到舊，只含資料庫內保留天數內的紀錄）。
    /// 以主表的 Id 找出歷程，並列出主表該筆資料與關聯表中 {主表}Id 相符的異動；
    /// 依賴「修改時一律更新 UpdatedAt」，確保只改關聯表的異動也會含主表資料
    /// </summary>
    /// <param name="tableName">主表名稱（如 BasicUser）</param>
    /// <param name="recordId">主表的 Id</param>
    /// <param name="relatedTableNames">一併顯示的關聯表（如 BasicUserGroup）</param>
    Task<BasicSyslogHistoryResult> GetRecordHistoryAsync(string tableName, int recordId, params string[] relatedTableNames);
}

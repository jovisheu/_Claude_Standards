namespace backend.Services.BasicSyslogs;

public interface IBasicSyslogsService
{
    /// <summary>
    /// 寫入異動歷程前呼叫：將早於「今天 - 保留天數」的紀錄轉存為 txt 後刪除。
    /// 同一天確認過即略過（新紀錄皆為當下時間，當天不會再出現過期紀錄）；
    /// 本身失敗時寫入系統錯誤紀錄、稍後再試，不丟出例外（不影響使用者的異動）
    /// </summary>
    Task ArchiveIfDueAsync();
}

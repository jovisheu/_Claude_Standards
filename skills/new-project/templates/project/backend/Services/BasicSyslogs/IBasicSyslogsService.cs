namespace backend.Services.BasicSyslogs;

public interface IBasicSyslogsService
{
    /// <summary>
    /// 將超過保留天數的歷程轉存為 Logs\Syslog\YYYY\MM\DD\{MethodName}.txt 後刪除；
    /// 同一天確認過即略過，同一時間只允許一個轉存（進行中時直接略過，不讓使用者的存檔等待）；
    /// 失敗寫入系統錯誤紀錄、10 分鐘後再試，不丟出例外。不接受取消，避免寫檔後、刪除前被中斷而重複轉存
    /// </summary>
    Task ArchiveIfDueAsync();
}

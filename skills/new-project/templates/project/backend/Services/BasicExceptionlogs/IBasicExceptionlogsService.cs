namespace backend.Services.BasicExceptionlogs;

public interface IBasicExceptionlogsService
{
    /// <summary>
    /// 寫入系統錯誤紀錄；本身失敗時只寫入 Log，不丟出例外。
    /// message 有值時取代例外訊息（例外訊息可能含個資時使用）
    /// </summary>
    Task WriteAsync(Exception exception, string methodName, string? username, string? message = null);

    /// <summary>將超過保留天數的紀錄轉存為 txt 後刪除，回傳轉存筆數</summary>
    Task<int> ArchiveAsync(CancellationToken cancellationToken);
}

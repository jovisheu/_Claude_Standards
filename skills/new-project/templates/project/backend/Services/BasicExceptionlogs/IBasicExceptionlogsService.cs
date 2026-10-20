namespace backend.Services.BasicExceptionlogs;

public interface IBasicExceptionlogsService
{
    /// <summary>
    /// 寫入系統錯誤紀錄（另建 DbContext，不沿用請求中可能含失敗變更的 DbContext）；寫入失敗只寫 ILogger、不再丟出例外。
    /// 例外訊息可能含個資時（如郵件寄送）以 message 取代原訊息。
    /// </summary>
    Task WriteAsync(Exception exception, string methodName, string? username = null, string? message = null);

    /// <summary>將超過保留天數的紀錄轉存為 Logs\Exception\YYYY\MM\DD\{MethodName}.txt，寫檔成功才刪除</summary>
    Task ArchiveAsync(CancellationToken cancellationToken);
}

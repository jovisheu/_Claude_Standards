namespace backend.Exceptions;

/// <summary>
/// 可顯示給使用者的業務錯誤，由全域例外處理回傳其訊息（不寫入系統錯誤紀錄）
/// </summary>
public class BusinessException(string message, int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

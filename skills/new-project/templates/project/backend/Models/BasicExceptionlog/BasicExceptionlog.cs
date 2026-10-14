namespace backend.Models;

/// <summary>
/// 系統錯誤紀錄（資料表 BasicExceptionlog；超過保留天數由背景服務轉存為 txt 後刪除）
/// </summary>
public class BasicExceptionlog
{
    public long Id { get; set; }

    /// <summary>例外類別（含 namespace，如 System.InvalidOperationException）</summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>發生位置（如 BasicUsersController.Create），同時作為轉存的檔名</summary>
    public string MethodName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }

    /// <summary>登入者帳號；未登入或背景作業為 null</summary>
    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; }
}

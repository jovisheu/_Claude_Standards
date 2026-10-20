namespace backend.Models;

/// <summary>系統錯誤紀錄</summary>
public class BasicExceptionlog
{
    public long Id { get; set; }

    public string ExceptionType { get; set; } = string.Empty;

    public string MethodName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? StackTrace { get; set; }

    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; }
}

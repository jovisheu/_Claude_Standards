namespace backend.Models;

/// <summary>密碼即將到期通知寄送紀錄（用於判斷當日是否已寄送）</summary>
public class BasicChecklogintimeSendMailLog
{
    public long Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

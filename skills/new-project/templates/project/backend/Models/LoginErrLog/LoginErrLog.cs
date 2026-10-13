namespace backend.Models;

/// <summary>
/// 登入失敗紀錄（資料表 _LoginErr_log，用於鎖定判斷）
/// </summary>
public class LoginErrLog
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

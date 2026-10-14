namespace backend.Models;

/// <summary>
/// 登入失敗紀錄（資料表 BasicLoginErrlog，用於鎖定判斷）
/// </summary>
public class BasicLoginErrlog
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

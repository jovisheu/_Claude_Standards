namespace backend.Models;

/// <summary>
/// 密碼歷程（資料表 BasicPwdlog，用於重複與到期判斷）
/// </summary>
public class BasicPwdlog
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

namespace backend.DTOs.Auth;

/// <summary>
/// 目前登入者資訊（權限即時取自資料庫）
/// </summary>
public class CurrentBasicUserResult
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> BasicPermissionNames { get; set; } = [];
}

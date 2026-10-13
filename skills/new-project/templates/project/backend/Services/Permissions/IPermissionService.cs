namespace backend.Services.Permissions;

public interface IPermissionService
{
    /// <summary>取得使用者目前的權限（停用或不存在的使用者回傳空集合）</summary>
    Task<HashSet<string>> GetUserPermissionsAsync(int userId);

    /// <summary>取得使用者目前的權限 Id</summary>
    Task<HashSet<int>> GetUserPermissionIdsAsync(int userId);

    /// <summary>是否為系統管理員群組成員</summary>
    Task<bool> IsAdminAsync(int userId);
}

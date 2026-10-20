namespace backend.Services.BasicPermissions;

public interface IBasicPermissionsService
{
    /// <summary>取得使用者擁有的權限名稱（查資料庫）；使用者不存在或已停用時回 null</summary>
    Task<IReadOnlySet<string>?> GetBasicPermissionNamesAsync(int basicUserId);

    /// <summary>取得使用者擁有的權限編號；使用者不存在或已停用時回空集合</summary>
    Task<IReadOnlySet<int>> GetBasicPermissionIdsAsync(int basicUserId);

    /// <summary>是否為啟用中的系統管理員群組成員（系統管理員群組只能由系統管理員指派與管理）</summary>
    Task<bool> IsAdminAsync(int basicUserId);
}

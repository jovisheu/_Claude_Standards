namespace backend.Data;

/// <summary>
/// 權限名稱（{資料表名稱小寫}.{動作}），同時作為授權 Policy 名稱
/// </summary>
public static class PermissionNames
{
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";

    public const string GroupsRead = "groups.read";
    public const string GroupsCreate = "groups.create";
    public const string GroupsUpdate = "groups.update";
    public const string GroupsDelete = "groups.delete";

    /// <summary>全部權限，用於註冊授權 Policy</summary>
    public static readonly string[] All =
    [
        UsersRead, UsersCreate, UsersUpdate, UsersDelete,
        GroupsRead, GroupsCreate, GroupsUpdate, GroupsDelete
    ];
}

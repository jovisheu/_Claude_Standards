namespace backend.Data;

/// <summary>
/// 權限名稱（{資料表名稱小寫}.{動作}），同時作為授權 Policy 名稱
/// </summary>
public static class BasicPermissionNames
{
    public const string BasicUserRead = "basicuser.read";
    public const string BasicUserCreate = "basicuser.create";
    public const string BasicUserUpdate = "basicuser.update";
    public const string BasicUserDelete = "basicuser.delete";

    public const string BasicGroupRead = "basicgroup.read";
    public const string BasicGroupCreate = "basicgroup.create";
    public const string BasicGroupUpdate = "basicgroup.update";
    public const string BasicGroupDelete = "basicgroup.delete";

    /// <summary>全部權限，用於註冊授權 Policy</summary>
    public static readonly string[] All =
    [
        BasicUserRead, BasicUserCreate, BasicUserUpdate, BasicUserDelete,
        BasicGroupRead, BasicGroupCreate, BasicGroupUpdate, BasicGroupDelete
    ];
}

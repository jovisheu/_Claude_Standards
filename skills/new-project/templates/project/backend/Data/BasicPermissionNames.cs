namespace backend.Data;

/// <summary>
/// 權限名稱（同時為授權 Policy 名稱），格式為 {資料表小寫}.{read|create|update|delete}
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

    /// <summary>所有權限名稱（註冊授權 Policy 用），須與 SeedData.BasicPermission 一致</summary>
    public static readonly string[] All =
    [
        BasicUserRead, BasicUserCreate, BasicUserUpdate, BasicUserDelete,
        BasicGroupRead, BasicGroupCreate, BasicGroupUpdate, BasicGroupDelete,
    ];
}

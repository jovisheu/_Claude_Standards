using backend.Models;

namespace backend.Data;

/// <summary>
/// 靜態種子資料（隨 Migration 建表寫入）
/// </summary>
public static class SeedData
{
    /// <summary>受保護的系統管理員群組 Id</summary>
    public const int AdminBasicGroupId = 1;

    /// <summary>預設管理員 Id</summary>
    public const int AdminBasicUserId = 1;

    /// <summary>訪客群組 Id（擁有全部檢視權限）</summary>
    public const int GuestBasicGroupId = 2;

    /// <summary>預設一般使用者（訪客）Id</summary>
    public const int GuestBasicUserId = 2;

    private static readonly DateTime SeedTime = new(__SEED_YEAR__, __SEED_MONTH__, __SEED_DAY__, 0, 0, 0);

    /// <summary>
    /// 預設管理員初始密碼的 BCrypt 雜湊（程式與 Migration 只存雜湊）
    /// </summary>
    private const string AdminPasswordHash = "__ADMIN_PASSWORD_HASH__";

    /// <summary>
    /// 預設一般使用者（訪客）初始密碼的 BCrypt 雜湊
    /// </summary>
    private const string GuestPasswordHash = "__GUEST_PASSWORD_HASH__";

    public static readonly BasicPermission[] BasicPermission =
    [
        new() { Id = 1, Name = BasicPermissionNames.BasicUserRead, Description = "檢視使用者", Module = "使用者管理" },
        new() { Id = 2, Name = BasicPermissionNames.BasicUserCreate, Description = "新增使用者", Module = "使用者管理" },
        new() { Id = 3, Name = BasicPermissionNames.BasicUserUpdate, Description = "編輯使用者", Module = "使用者管理" },
        new() { Id = 4, Name = BasicPermissionNames.BasicUserDelete, Description = "刪除使用者", Module = "使用者管理" },
        new() { Id = 5, Name = BasicPermissionNames.BasicGroupRead, Description = "檢視群組", Module = "群組管理" },
        new() { Id = 6, Name = BasicPermissionNames.BasicGroupCreate, Description = "新增群組", Module = "群組管理" },
        new() { Id = 7, Name = BasicPermissionNames.BasicGroupUpdate, Description = "編輯群組", Module = "群組管理" },
        new() { Id = 8, Name = BasicPermissionNames.BasicGroupDelete, Description = "刪除群組", Module = "群組管理" }
    ];

    public static readonly BasicGroup[] BasicGroup =
    [
        new() { Id = AdminBasicGroupId, Name = "系統管理員", Description = "擁有全部權限（受保護）", CreatedAt = SeedTime },
        new() { Id = GuestBasicGroupId, Name = "訪客", Description = "只能檢視", CreatedAt = SeedTime }
    ];

    /// <summary>
    /// 系統管理員擁有全部權限；訪客擁有全部檢視（.read）權限，新增業務功能權限時自動套用
    /// </summary>
    public static readonly BasicGroupPermission[] BasicGroupPermission =
        BasicPermission.Select(p => new BasicGroupPermission { BasicGroupId = AdminBasicGroupId, BasicPermissionId = p.Id })
            .Concat(BasicPermission
                .Where(p => p.Name.EndsWith(".read", StringComparison.Ordinal))
                .Select(p => new BasicGroupPermission { BasicGroupId = GuestBasicGroupId, BasicPermissionId = p.Id }))
            .ToArray();

    public static readonly BasicUser[] BasicUser =
    [
        new()
        {
            Id = AdminBasicUserId,
            Username = "__ADMIN_USERNAME__",
            Email = "__ADMIN_EMAIL__",
            DisplayName = "__ADMIN_DISPLAY_NAME__",
            PasswordHash = AdminPasswordHash,
            IsActive = true,
            CreatedAt = SeedTime
        },
        new()
        {
            Id = GuestBasicUserId,
            Username = "__GUEST_USERNAME__",
            Email = "__GUEST_EMAIL__",
            DisplayName = "__GUEST_DISPLAY_NAME__",
            PasswordHash = GuestPasswordHash,
            IsActive = true,
            CreatedAt = SeedTime
        }
    ];

    public static readonly BasicUserGroup[] BasicUserGroup =
    [
        new() { BasicUserId = AdminBasicUserId, BasicGroupId = AdminBasicGroupId },
        new() { BasicUserId = GuestBasicUserId, BasicGroupId = GuestBasicGroupId }
    ];

    /// <summary>
    /// 初始密碼歷程設為建立專案當日，首次登入不強制變更，依密碼有效天數正常到期（無歷程會被視為逾期，不可省略）
    /// </summary>
    public static readonly BasicPwdlog[] BasicPwdlog =
    [
        new() { Id = 1, Username = "__ADMIN_USERNAME__", PasswordHash = AdminPasswordHash, CreatedAt = SeedTime },
        new() { Id = 2, Username = "__GUEST_USERNAME__", PasswordHash = GuestPasswordHash, CreatedAt = SeedTime }
    ];
}

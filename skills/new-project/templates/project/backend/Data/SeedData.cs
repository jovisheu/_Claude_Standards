using backend.Models;

namespace backend.Data;

/// <summary>
/// 靜態種子資料（隨 Migration 建表寫入）
/// </summary>
public static class SeedData
{
    /// <summary>受保護的系統管理員群組 Id</summary>
    public const int AdminGroupId = 1;

    /// <summary>預設管理員 Id</summary>
    public const int AdminUserId = 1;

    private static readonly DateTime SeedTime = new(__SEED_YEAR__, __SEED_MONTH__, __SEED_DAY__, 0, 0, 0);

    /// <summary>
    /// 預設管理員初始密碼的 BCrypt 雜湊（程式與 Migration 只存雜湊）
    /// </summary>
    private const string AdminPasswordHash = "__ADMIN_PASSWORD_HASH__";

    public static readonly Permission[] Permissions =
    [
        new() { Id = 1, Name = PermissionNames.UsersRead, Description = "檢視使用者", Module = "使用者管理" },
        new() { Id = 2, Name = PermissionNames.UsersCreate, Description = "新增使用者", Module = "使用者管理" },
        new() { Id = 3, Name = PermissionNames.UsersUpdate, Description = "編輯使用者", Module = "使用者管理" },
        new() { Id = 4, Name = PermissionNames.UsersDelete, Description = "刪除使用者", Module = "使用者管理" },
        new() { Id = 5, Name = PermissionNames.GroupsRead, Description = "檢視群組", Module = "群組管理" },
        new() { Id = 6, Name = PermissionNames.GroupsCreate, Description = "新增群組", Module = "群組管理" },
        new() { Id = 7, Name = PermissionNames.GroupsUpdate, Description = "編輯群組", Module = "群組管理" },
        new() { Id = 8, Name = PermissionNames.GroupsDelete, Description = "刪除群組", Module = "群組管理" }
    ];

    public static readonly Group[] Groups =
    [
        new() { Id = AdminGroupId, Name = "系統管理員", Description = "擁有全部權限（受保護）", CreatedAt = SeedTime }
    ];

    public static readonly GroupPermission[] GroupPermissions =
        Permissions.Select(p => new GroupPermission { GroupId = AdminGroupId, PermissionId = p.Id }).ToArray();

    public static readonly User[] Users =
    [
        new()
        {
            Id = AdminUserId,
            Username = "__ADMIN_USERNAME__",
            Email = "__ADMIN_EMAIL__",
            DisplayName = "__ADMIN_DISPLAY_NAME__",
            PasswordHash = AdminPasswordHash,
            IsActive = true,
            CreatedAt = SeedTime
        }
    ];

    public static readonly UserGroup[] UserGroups =
    [
        new() { UserId = AdminUserId, GroupId = AdminGroupId }
    ];

    /// <summary>
    /// 初始密碼歷程設為建立專案當日，首次登入不強制變更，依密碼有效天數正常到期（無歷程會被視為逾期，不可省略）
    /// </summary>
    public static readonly PwdLog[] PwdLogs =
    [
        new() { Id = 1, Username = "__ADMIN_USERNAME__", PasswordHash = AdminPasswordHash, CreatedAt = SeedTime }
    ];
}

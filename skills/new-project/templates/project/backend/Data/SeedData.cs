using backend.Models;

namespace backend.Data;

/// <summary>
/// 靜態種子（隨 Migration 建表時寫入）；密碼只存 BCrypt 雜湊，不出現明文
/// </summary>
public static class SeedData
{
    public const int AdminBasicGroupId = 1;
    public const int GuestBasicGroupId = 2;
    public const int AdminBasicUserId = 1;
    public const int GuestBasicUserId = 2;

    /// <summary>建立專案當日；種子的建立時間與初始密碼歷程日期（首次登入不強制變更密碼）</summary>
    private static readonly DateTime SeedDate = new(__SEED_YEAR__, __SEED_MONTH__, __SEED_DAY__);

    private const string AdminUsername = "__ADMIN_USERNAME__";
    private const string GuestUsername = "__GUEST_USERNAME__";
    private const string AdminPasswordHash = "__ADMIN_PASSWORD_HASH__";
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
        new() { Id = 8, Name = BasicPermissionNames.BasicGroupDelete, Description = "刪除群組", Module = "群組管理" },
    ];

    public static readonly BasicGroup[] BasicGroup =
    [
        new() { Id = AdminBasicGroupId, Name = "系統管理員", Description = "擁有全部權限", CreatedAt = SeedDate },
        new() { Id = GuestBasicGroupId, Name = "訪客", Description = "只能檢視", CreatedAt = SeedDate },
    ];

    /// <summary>系統管理員群組擁有全部權限；訪客群組擁有全部檢視（.read）權限</summary>
    public static readonly BasicGroupPermission[] BasicGroupPermission =
    [
        .. BasicPermission.Select(p => new BasicGroupPermission { BasicGroupId = AdminBasicGroupId, BasicPermissionId = p.Id }),
        .. BasicPermission.Where(p => p.Name.EndsWith(".read", StringComparison.Ordinal))
            .Select(p => new BasicGroupPermission { BasicGroupId = GuestBasicGroupId, BasicPermissionId = p.Id }),
    ];

    public static readonly BasicUser[] BasicUser =
    [
        new()
        {
            Id = AdminBasicUserId,
            Username = AdminUsername,
            Email = "__ADMIN_EMAIL__",
            DisplayName = "__ADMIN_DISPLAY_NAME__",
            PasswordHash = AdminPasswordHash,
            IsActive = true,
            CreatedAt = SeedDate,
        },
        new()
        {
            Id = GuestBasicUserId,
            Username = GuestUsername,
            Email = "__GUEST_EMAIL__",
            DisplayName = "__GUEST_DISPLAY_NAME__",
            PasswordHash = GuestPasswordHash,
            IsActive = true,
            CreatedAt = SeedDate,
        },
    ];

    public static readonly BasicUserGroup[] BasicUserGroup =
    [
        new() { BasicUserId = AdminBasicUserId, BasicGroupId = AdminBasicGroupId },
        new() { BasicUserId = GuestBasicUserId, BasicGroupId = GuestBasicGroupId },
    ];

    /// <summary>初始密碼歷程（無歷程會被視為逾期，不可省略）</summary>
    public static readonly BasicPwdlog[] BasicPwdlog =
    [
        new() { Id = 1, Username = AdminUsername, PasswordHash = AdminPasswordHash, CreatedAt = SeedDate },
        new() { Id = 2, Username = GuestUsername, PasswordHash = GuestPasswordHash, CreatedAt = SeedDate },
    ];
}

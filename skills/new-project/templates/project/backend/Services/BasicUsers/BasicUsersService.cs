using backend.Data;
using backend.DTOs;
using backend.DTOs.BasicUsers;
using backend.Exceptions;
using backend.Models;
using backend.Services.BasicPermissions;
using backend.Services.Password;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.BasicUsers;

/// <summary>
/// 使用者管理：不可越權（對方權限超過操作者時不可管理、不可指派超過自身權限的群組；系統管理員群組只能由系統管理員指派與管理）、
/// 不可停用或刪除自己、不可變更自己的群組、系統管理員群組至少保留一位啟用中的成員
/// </summary>
public class BasicUsersService(
    AppDbContext db,
    IPasswordService passwordService,
    IBasicPermissionsService basicPermissionsService,
    ILogger<BasicUsersService> logger) : IBasicUsersService
{
    private const string NotFoundMessage = "找不到指定的使用者";

    public async Task<PagedResult<BasicUserListItem>> GetPagedAsync(PagedQuery query, int operatorId)
    {
        var source = db.BasicUser.AsNoTracking();
        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            source = source.Where(u => u.Username.Contains(keyword) || u.DisplayName.Contains(keyword) || u.Email.Contains(keyword));
        }

        var totalCount = await source.CountAsync();
        var rows = await source
            .OrderBy(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.DisplayName,
                u.IsActive,
                u.CreatedAt,
                u.UpdatedAt,
                BasicGroupIds = u.BasicUserGroup.Select(ug => ug.BasicGroupId).ToList(),
                BasicGroupNames = u.BasicUserGroup.OrderBy(ug => ug.BasicGroupId).Select(ug => ug.BasicGroup!.Name).ToList(),
            })
            .ToListAsync();

        var context = await LoadPermissionContextAsync(operatorId);
        return new PagedResult<BasicUserListItem>
        {
            Items = rows.Select(r => new BasicUserListItem
            {
                Id = r.Id,
                Username = r.Username,
                Email = r.Email,
                DisplayName = r.DisplayName,
                IsActive = r.IsActive,
                BasicGroupNames = r.BasicGroupNames,
                IsManageable = context.CanManage(r.BasicGroupIds),
                IsSelf = r.Id == operatorId,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            }).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<BasicUserDetail> GetAsync(int id, int operatorId)
    {
        var basicUser = await db.BasicUser.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.DisplayName,
                u.IsActive,
                u.CreatedAt,
                u.UpdatedAt,
                BasicGroupIds = u.BasicUserGroup.Select(ug => ug.BasicGroupId).ToList(),
            })
            .FirstOrDefaultAsync()
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        var context = await LoadPermissionContextAsync(operatorId);
        return new BasicUserDetail
        {
            Id = basicUser.Id,
            Username = basicUser.Username,
            Email = basicUser.Email,
            DisplayName = basicUser.DisplayName,
            IsActive = basicUser.IsActive,
            BasicGroupIds = basicUser.BasicGroupIds,
            IsManageable = context.CanManage(basicUser.BasicGroupIds),
            IsSelf = basicUser.Id == operatorId,
            CreatedAt = basicUser.CreatedAt,
            UpdatedAt = basicUser.UpdatedAt,
        };
    }

    public async Task<List<BasicGroupOption>> GetBasicGroupOptionsAsync(int operatorId)
    {
        var context = await LoadPermissionContextAsync(operatorId);
        var options = await db.BasicGroup.AsNoTracking()
            .OrderBy(g => g.Id)
            .Select(g => new BasicGroupOption { Id = g.Id, Name = g.Name })
            .ToListAsync();
        foreach (var option in options)
        {
            option.IsAssignable = context.CanAssign(option.Id);
        }

        return options;
    }

    public async Task<int> CreateAsync(BasicUserCreateRequest request, int operatorId)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await db.BasicUser.AnyAsync(u => u.Username == username))
        {
            throw new BusinessException("帳號已存在");
        }

        if (await db.BasicUser.AnyAsync(u => u.Email == email))
        {
            throw new BusinessException("Email 已被使用");
        }

        var context = await LoadPermissionContextAsync(operatorId);
        var basicGroupIds = request.BasicGroupIds.Distinct().ToList();
        EnsureGroupsExist(context, basicGroupIds);
        if (basicGroupIds.Any(id => !context.CanAssign(id)))
        {
            throw new BusinessException("不可指派超過自身權限的群組", StatusCodes.Status403Forbidden);
        }

        var basicUser = new BasicUser
        {
            Username = username,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now,
        };
        foreach (var basicGroupId in basicGroupIds)
        {
            basicUser.BasicUserGroup.Add(new BasicUserGroup { BasicGroupId = basicGroupId });
        }

        await passwordService.SetPasswordAsync(basicUser, request.Password);
        db.BasicUser.Add(basicUser);
        await db.SaveChangesAsync();
        logger.LogInformation("新增使用者：{BasicUserId}，操作者：{OperatorId}", basicUser.Id, operatorId);
        return basicUser.Id;
    }

    public async Task UpdateAsync(int id, BasicUserUpdateRequest request, int operatorId)
    {
        var basicUser = await db.BasicUser.Include(u => u.BasicUserGroup).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        var context = await LoadPermissionContextAsync(operatorId);
        var currentGroupIds = basicUser.BasicUserGroup.Select(ug => ug.BasicGroupId).ToHashSet();
        EnsureManageable(context, currentGroupIds);

        var email = request.Email.Trim();
        if (await db.BasicUser.AnyAsync(u => u.Email == email && u.Id != id))
        {
            throw new BusinessException("Email 已被使用");
        }

        var newGroupIds = request.BasicGroupIds.Distinct().ToHashSet();
        EnsureGroupsExist(context, newGroupIds);

        var isSelf = id == operatorId;
        if (isSelf && !request.IsActive)
        {
            throw new BusinessException("不可停用自己");
        }

        if (isSelf && !newGroupIds.SetEquals(currentGroupIds))
        {
            throw new BusinessException("不可變更自己的群組");
        }

        // 新增或移除的群組都須在操作者權限內
        var changedGroupIds = newGroupIds.Except(currentGroupIds).Union(currentGroupIds.Except(newGroupIds));
        if (changedGroupIds.Any(groupId => !context.CanAssign(groupId)))
        {
            throw new BusinessException("不可指派或移除超過自身權限的群組", StatusCodes.Status403Forbidden);
        }

        await EnsureAdminGroupRemainsAsync(basicUser, currentGroupIds, request.IsActive && newGroupIds.Contains(SeedData.AdminBasicGroupId));

        basicUser.Email = email;
        basicUser.DisplayName = request.DisplayName.Trim();
        basicUser.IsActive = request.IsActive;
        basicUser.UpdatedAt = DateTime.Now;

        foreach (var userGroup in basicUser.BasicUserGroup.Where(ug => !newGroupIds.Contains(ug.BasicGroupId)).ToList())
        {
            basicUser.BasicUserGroup.Remove(userGroup);
        }

        foreach (var basicGroupId in newGroupIds.Except(currentGroupIds))
        {
            basicUser.BasicUserGroup.Add(new BasicUserGroup { BasicUserId = id, BasicGroupId = basicGroupId });
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            await passwordService.SetPasswordAsync(basicUser, request.Password);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("編輯使用者：{BasicUserId}，操作者：{OperatorId}", id, operatorId);
    }

    public async Task DeleteAsync(int id, int operatorId)
    {
        if (id == operatorId)
        {
            throw new BusinessException("不可刪除自己");
        }

        var basicUser = await db.BasicUser.Include(u => u.BasicUserGroup).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        var context = await LoadPermissionContextAsync(operatorId);
        var currentGroupIds = basicUser.BasicUserGroup.Select(ug => ug.BasicGroupId).ToHashSet();
        EnsureManageable(context, currentGroupIds);
        await EnsureAdminGroupRemainsAsync(basicUser, currentGroupIds, willBeActiveAdmin: false);

        // 關聯一併刪除（同一次存檔，異動歷程含關聯表）
        db.BasicUserGroup.RemoveRange(basicUser.BasicUserGroup);
        db.BasicUser.Remove(basicUser);
        await db.SaveChangesAsync();
        logger.LogInformation("刪除使用者：{BasicUserId}，操作者：{OperatorId}", id, operatorId);
    }

    private static void EnsureManageable(PermissionContext context, IEnumerable<int> basicGroupIds)
    {
        if (!context.CanManage(basicGroupIds))
        {
            throw new BusinessException("此使用者的權限超過您的權限，無法管理", StatusCodes.Status403Forbidden);
        }
    }

    private static void EnsureGroupsExist(PermissionContext context, IEnumerable<int> basicGroupIds)
    {
        if (basicGroupIds.Any(id => !context.GroupPermissionIds.ContainsKey(id)))
        {
            throw new BusinessException("指定的群組不存在");
        }
    }

    /// <summary>系統管理員群組至少保留一位啟用中的成員</summary>
    private async Task EnsureAdminGroupRemainsAsync(BasicUser basicUser, HashSet<int> currentGroupIds, bool willBeActiveAdmin)
    {
        var isActiveAdmin = basicUser.IsActive && currentGroupIds.Contains(SeedData.AdminBasicGroupId);
        if (!isActiveAdmin || willBeActiveAdmin)
        {
            return;
        }

        var others = await db.BasicUserGroup.AnyAsync(ug =>
            ug.BasicGroupId == SeedData.AdminBasicGroupId && ug.BasicUserId != basicUser.Id && ug.BasicUser!.IsActive);
        if (!others)
        {
            throw new BusinessException("系統管理員群組至少須保留一位啟用中的成員");
        }
    }

    private async Task<PermissionContext> LoadPermissionContextAsync(int operatorId)
    {
        var operatorPermissionIds = await basicPermissionsService.GetBasicPermissionIdsAsync(operatorId);
        var isAdmin = await basicPermissionsService.IsAdminAsync(operatorId);
        var groupIds = await db.BasicGroup.AsNoTracking().Select(g => g.Id).ToListAsync();
        var pairs = (await db.BasicGroupPermission.AsNoTracking()
                .Select(gp => new { gp.BasicGroupId, gp.BasicPermissionId })
                .ToListAsync())
            .ToLookup(p => p.BasicGroupId, p => p.BasicPermissionId);

        var groupPermissionIds = groupIds.ToDictionary(id => id, id => pairs[id].ToHashSet());
        return new PermissionContext(operatorPermissionIds, isAdmin, groupPermissionIds);
    }

    /// <summary>
    /// 操作者權限與各群組權限，判斷可否指派群組或管理使用者；
    /// 系統管理員群組只能由系統管理員指派，其成員也只能由系統管理員管理（即使操作者擁有全部權限）
    /// </summary>
    private sealed record PermissionContext(IReadOnlySet<int> OperatorPermissionIds, bool IsAdmin, Dictionary<int, HashSet<int>> GroupPermissionIds)
    {
        public bool CanAssign(int basicGroupId)
            => (basicGroupId != SeedData.AdminBasicGroupId || IsAdmin)
               && GroupPermissionIds.TryGetValue(basicGroupId, out var permissionIds)
               && permissionIds.IsSubsetOf(OperatorPermissionIds);

        public bool CanManage(IEnumerable<int> basicGroupIds)
            => basicGroupIds.All(id => !GroupPermissionIds.ContainsKey(id) || CanAssign(id));
    }
}

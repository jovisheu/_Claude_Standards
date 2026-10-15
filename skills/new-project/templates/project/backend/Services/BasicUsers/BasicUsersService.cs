using backend.Data;
using backend.DTOs;
using backend.DTOs.BasicUsers;
using backend.Exceptions;
using backend.Models;
using backend.Services.Password;
using backend.Services.BasicPermissions;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.BasicUsers;

public class BasicUsersService(
    AppDbContext db,
    IPasswordService passwordService,
    IBasicPermissionsService basicPermissionsService,
    ILogger<BasicUsersService> logger) : IBasicUsersService
{
    private const string NotFoundMessage = "使用者不存在";

    public async Task<PagedResult<BasicUserListItem>> GetPagedAsync(PagedQuery query)
    {
        var basicUsers = db.BasicUser.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            basicUsers = basicUsers.Where(u => u.Username.Contains(keyword) || u.DisplayName.Contains(keyword) || u.Email.Contains(keyword));
        }

        var totalCount = await basicUsers.CountAsync();
        var items = await basicUsers
            .OrderBy(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new BasicUserListItem
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                DisplayName = u.DisplayName,
                IsActive = u.IsActive,
                BasicGroupNames = u.BasicUserGroup.OrderBy(ug => ug.BasicGroupId).Select(ug => ug.BasicGroup.Name).ToList(),
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

        return new PagedResult<BasicUserListItem> { Items = items, TotalCount = totalCount, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<BasicUserDetail> GetAsync(int id)
    {
        return await db.BasicUser.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new BasicUserDetail
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                DisplayName = u.DisplayName,
                IsActive = u.IsActive,
                BasicGroupIds = u.BasicUserGroup.Select(ug => ug.BasicGroupId).ToList(),
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .FirstOrDefaultAsync()
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);
    }

    public async Task<List<BasicGroupOption>> GetBasicGroupOptionsAsync(int operatorId)
    {
        var operatorBasicPermissionIds = await basicPermissionsService.GetBasicUserPermissionIdsAsync(operatorId);
        var operatorIsAdmin = await basicPermissionsService.IsAdminAsync(operatorId);

        var basicGroups = await db.BasicGroup.AsNoTracking()
            .OrderBy(g => g.Id)
            .Select(g => new { g.Id, g.Name, BasicPermissionIds = g.BasicGroupPermission.Select(gp => gp.BasicPermissionId).ToList() })
            .ToListAsync();

        return basicGroups.Select(g => new BasicGroupOption
        {
            Id = g.Id,
            Name = g.Name,
            Assignable = CanAssignBasicGroup(g.Id, g.BasicPermissionIds, operatorBasicPermissionIds, operatorIsAdmin)
        }).ToList();
    }

    public async Task<int> CreateAsync(BasicUserCreateRequest request, int operatorId)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        await EnsureUniqueAsync(username, email, null);
        var basicGroupIds = await ValidateAssignableBasicGroupsAsync(request.BasicGroupIds, operatorId);

        var basicUser = new BasicUser
        {
            Username = username,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now
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

        await EnsureCanManageAsync(basicUser, operatorId);

        var email = request.Email.Trim();
        await EnsureUniqueAsync(basicUser.Username, email, id);

        var currentBasicGroupIds = basicUser.BasicUserGroup.Select(ug => ug.BasicGroupId).ToHashSet();
        var newBasicGroupIds = request.BasicGroupIds.Distinct().ToHashSet();
        var basicGroupsChanged = !currentBasicGroupIds.SetEquals(newBasicGroupIds);

        if (id == operatorId)
        {
            if (!request.IsActive) throw new BusinessException("不可停用自己的帳號");
            if (basicGroupsChanged) throw new BusinessException("不可變更自己的群組");
        }

        if (basicGroupsChanged)
        {
            // 只檢查新增的群組是否可指派；移除既有群組須具備管理該群組的權限
            var addedBasicGroupIds = newBasicGroupIds.Except(currentBasicGroupIds).ToList();
            var removedBasicGroupIds = currentBasicGroupIds.Except(newBasicGroupIds).ToList();
            await ValidateAssignableBasicGroupsAsync(addedBasicGroupIds, operatorId);
            await ValidateAssignableBasicGroupsAsync(removedBasicGroupIds, operatorId);
        }

        // 系統管理員群組至少保留一位啟用中的成員
        var willLeaveAdmin = currentBasicGroupIds.Contains(SeedData.AdminBasicGroupId)
            && (!request.IsActive || !newBasicGroupIds.Contains(SeedData.AdminBasicGroupId));
        if (willLeaveAdmin) await EnsureOtherActiveAdminAsync(id);

        basicUser.Email = email;
        basicUser.DisplayName = request.DisplayName.Trim();
        basicUser.IsActive = request.IsActive;
        basicUser.UpdatedAt = DateTime.Now;

        if (basicGroupsChanged)
        {
            foreach (var basicUserGroup in basicUser.BasicUserGroup.Where(ug => !newBasicGroupIds.Contains(ug.BasicGroupId)).ToList())
            {
                basicUser.BasicUserGroup.Remove(basicUserGroup);
            }
            foreach (var basicGroupId in newBasicGroupIds.Except(currentBasicGroupIds))
            {
                basicUser.BasicUserGroup.Add(new BasicUserGroup { BasicUserId = id, BasicGroupId = basicGroupId });
            }
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
        if (id == operatorId) throw new BusinessException("不可刪除自己的帳號");

        var basicUser = await db.BasicUser.Include(u => u.BasicUserGroup).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        await EnsureCanManageAsync(basicUser, operatorId);

        if (basicUser.BasicUserGroup.Any(ug => ug.BasicGroupId == SeedData.AdminBasicGroupId))
        {
            await EnsureOtherActiveAdminAsync(id);
        }

        db.BasicUser.Remove(basicUser);
        await db.SaveChangesAsync();
        logger.LogInformation("刪除使用者：{BasicUserId}，操作者：{OperatorId}", id, operatorId);
    }

    private async Task EnsureUniqueAsync(string username, string email, int? excludeId)
    {
        if (excludeId == null && await db.BasicUser.AnyAsync(u => u.Username == username))
        {
            throw new BusinessException("帳號已存在");
        }
        if (await db.BasicUser.AnyAsync(u => u.Email == email && u.Id != excludeId))
        {
            throw new BusinessException("Email 已被使用");
        }
    }

    /// <summary>
    /// 不可管理權限高於自己的使用者；系統管理員群組成員只能由系統管理員管理
    /// </summary>
    private async Task EnsureCanManageAsync(BasicUser target, int operatorId)
    {
        if (target.Id == operatorId) return;

        var operatorIsAdmin = await basicPermissionsService.IsAdminAsync(operatorId);
        if (operatorIsAdmin) return;

        if (target.BasicUserGroup.Any(ug => ug.BasicGroupId == SeedData.AdminBasicGroupId))
        {
            throw new BusinessException("無法管理系統管理員", StatusCodes.Status403Forbidden);
        }

        var operatorBasicPermissionIds = await basicPermissionsService.GetBasicUserPermissionIdsAsync(operatorId);
        var targetBasicGroupIds = target.BasicUserGroup.Select(ug => ug.BasicGroupId).ToList();
        var targetBasicPermissionIds = await db.BasicGroupPermission
            .Where(gp => targetBasicGroupIds.Contains(gp.BasicGroupId))
            .Select(gp => gp.BasicPermissionId)
            .Distinct()
            .ToListAsync();

        if (!targetBasicPermissionIds.All(operatorBasicPermissionIds.Contains))
        {
            throw new BusinessException("無法管理權限高於自己的使用者", StatusCodes.Status403Forbidden);
        }
    }

    /// <summary>
    /// 群組去除重複並確認存在，且不可授予高於自身的權限
    /// </summary>
    private async Task<List<int>> ValidateAssignableBasicGroupsAsync(IEnumerable<int> basicGroupIds, int operatorId)
    {
        var ids = basicGroupIds.Distinct().ToList();
        if (ids.Count == 0) return ids;

        var basicGroups = await db.BasicGroup.AsNoTracking()
            .Where(g => ids.Contains(g.Id))
            .Select(g => new { g.Id, BasicPermissionIds = g.BasicGroupPermission.Select(gp => gp.BasicPermissionId).ToList() })
            .ToListAsync();

        if (basicGroups.Count != ids.Count) throw new BusinessException("群組不存在");

        var operatorBasicPermissionIds = await basicPermissionsService.GetBasicUserPermissionIdsAsync(operatorId);
        var operatorIsAdmin = await basicPermissionsService.IsAdminAsync(operatorId);

        if (basicGroups.Any(g => !CanAssignBasicGroup(g.Id, g.BasicPermissionIds, operatorBasicPermissionIds, operatorIsAdmin)))
        {
            throw new BusinessException("不可指派權限高於自己的群組", StatusCodes.Status403Forbidden);
        }

        return ids;
    }

    private static bool CanAssignBasicGroup(int basicGroupId, List<int> basicGroupPermissionIds, HashSet<int> operatorBasicPermissionIds, bool operatorIsAdmin)
    {
        if (operatorIsAdmin) return true;
        if (basicGroupId == SeedData.AdminBasicGroupId) return false;
        return basicGroupPermissionIds.All(operatorBasicPermissionIds.Contains);
    }

    private async Task EnsureOtherActiveAdminAsync(int excludeBasicUserId)
    {
        var hasOther = await db.BasicUserGroup.AnyAsync(ug =>
            ug.BasicGroupId == SeedData.AdminBasicGroupId && ug.BasicUserId != excludeBasicUserId && ug.BasicUser.IsActive);
        if (!hasOther)
        {
            throw new BusinessException("系統管理員群組至少須保留一位啟用中的成員");
        }
    }
}

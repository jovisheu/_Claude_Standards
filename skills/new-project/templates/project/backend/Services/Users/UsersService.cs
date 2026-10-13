using backend.Data;
using backend.DTOs;
using backend.DTOs.Users;
using backend.Exceptions;
using backend.Models;
using backend.Services.Password;
using backend.Services.Permissions;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Users;

public class UsersService(
    AppDbContext db,
    IPasswordService passwordService,
    IPermissionService permissionService,
    ILogger<UsersService> logger) : IUsersService
{
    private const string NotFoundMessage = "使用者不存在";

    public async Task<PagedResult<UserListItem>> GetPagedAsync(PagedQuery query)
    {
        var users = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            users = users.Where(u => u.Username.Contains(keyword) || u.DisplayName.Contains(keyword) || u.Email.Contains(keyword));
        }

        var totalCount = await users.CountAsync();
        var items = await users
            .OrderBy(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserListItem
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                DisplayName = u.DisplayName,
                IsActive = u.IsActive,
                GroupNames = u.UserGroups.OrderBy(ug => ug.GroupId).Select(ug => ug.Group.Name).ToList(),
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<UserListItem> { Items = items, TotalCount = totalCount, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<UserDetail> GetAsync(int id)
    {
        return await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDetail
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                DisplayName = u.DisplayName,
                IsActive = u.IsActive,
                GroupIds = u.UserGroups.Select(ug => ug.GroupId).ToList(),
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .FirstOrDefaultAsync()
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);
    }

    public async Task<List<GroupOption>> GetGroupOptionsAsync(int operatorId)
    {
        var operatorPermissionIds = await permissionService.GetUserPermissionIdsAsync(operatorId);
        var operatorIsAdmin = await permissionService.IsAdminAsync(operatorId);

        var groups = await db.Groups.AsNoTracking()
            .OrderBy(g => g.Id)
            .Select(g => new { g.Id, g.Name, PermissionIds = g.GroupPermissions.Select(gp => gp.PermissionId).ToList() })
            .ToListAsync();

        return groups.Select(g => new GroupOption
        {
            Id = g.Id,
            Name = g.Name,
            Assignable = CanAssignGroup(g.Id, g.PermissionIds, operatorPermissionIds, operatorIsAdmin)
        }).ToList();
    }

    public async Task<int> CreateAsync(CreateUserRequest request, int operatorId)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        await EnsureUniqueAsync(username, email, null);
        var groupIds = await ValidateAssignableGroupsAsync(request.GroupIds, operatorId);

        var user = new User
        {
            Username = username,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now
        };
        foreach (var groupId in groupIds)
        {
            user.UserGroups.Add(new UserGroup { GroupId = groupId });
        }

        await passwordService.SetPasswordAsync(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        logger.LogInformation("新增使用者：{UserId}，操作者：{OperatorId}", user.Id, operatorId);
        return user.Id;
    }

    public async Task UpdateAsync(int id, UpdateUserRequest request, int operatorId)
    {
        var user = await db.Users.Include(u => u.UserGroups).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        await EnsureCanManageAsync(user, operatorId);

        var email = request.Email.Trim();
        await EnsureUniqueAsync(user.Username, email, id);

        var currentGroupIds = user.UserGroups.Select(ug => ug.GroupId).ToHashSet();
        var newGroupIds = request.GroupIds.Distinct().ToHashSet();
        var groupsChanged = !currentGroupIds.SetEquals(newGroupIds);

        if (id == operatorId)
        {
            if (!request.IsActive) throw new BusinessException("不可停用自己的帳號");
            if (groupsChanged) throw new BusinessException("不可變更自己的群組");
        }

        if (groupsChanged)
        {
            // 只檢查新增的群組是否可指派；移除既有群組須具備管理該群組的權限
            var addedGroupIds = newGroupIds.Except(currentGroupIds).ToList();
            var removedGroupIds = currentGroupIds.Except(newGroupIds).ToList();
            await ValidateAssignableGroupsAsync(addedGroupIds, operatorId);
            await ValidateAssignableGroupsAsync(removedGroupIds, operatorId);
        }

        // 系統管理員群組至少保留一位啟用中的成員
        var willLeaveAdmin = currentGroupIds.Contains(SeedData.AdminGroupId)
            && (!request.IsActive || !newGroupIds.Contains(SeedData.AdminGroupId));
        if (willLeaveAdmin) await EnsureOtherActiveAdminAsync(id);

        user.Email = email;
        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.Now;

        if (groupsChanged)
        {
            foreach (var userGroup in user.UserGroups.Where(ug => !newGroupIds.Contains(ug.GroupId)).ToList())
            {
                user.UserGroups.Remove(userGroup);
            }
            foreach (var groupId in newGroupIds.Except(currentGroupIds))
            {
                user.UserGroups.Add(new UserGroup { UserId = id, GroupId = groupId });
            }
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            await passwordService.SetPasswordAsync(user, request.Password);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("編輯使用者：{UserId}，操作者：{OperatorId}", id, operatorId);
    }

    public async Task DeleteAsync(int id, int operatorId)
    {
        if (id == operatorId) throw new BusinessException("不可刪除自己的帳號");

        var user = await db.Users.Include(u => u.UserGroups).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        await EnsureCanManageAsync(user, operatorId);

        if (user.UserGroups.Any(ug => ug.GroupId == SeedData.AdminGroupId))
        {
            await EnsureOtherActiveAdminAsync(id);
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        logger.LogInformation("刪除使用者：{UserId}，操作者：{OperatorId}", id, operatorId);
    }

    private async Task EnsureUniqueAsync(string username, string email, int? excludeId)
    {
        if (excludeId == null && await db.Users.AnyAsync(u => u.Username == username))
        {
            throw new BusinessException("帳號已存在");
        }
        if (await db.Users.AnyAsync(u => u.Email == email && u.Id != excludeId))
        {
            throw new BusinessException("Email 已被使用");
        }
    }

    /// <summary>
    /// 不可管理權限高於自己的使用者；系統管理員群組成員只能由系統管理員管理
    /// </summary>
    private async Task EnsureCanManageAsync(User target, int operatorId)
    {
        if (target.Id == operatorId) return;

        var operatorIsAdmin = await permissionService.IsAdminAsync(operatorId);
        if (operatorIsAdmin) return;

        if (target.UserGroups.Any(ug => ug.GroupId == SeedData.AdminGroupId))
        {
            throw new BusinessException("無法管理系統管理員", StatusCodes.Status403Forbidden);
        }

        var operatorPermissionIds = await permissionService.GetUserPermissionIdsAsync(operatorId);
        var targetGroupIds = target.UserGroups.Select(ug => ug.GroupId).ToList();
        var targetPermissionIds = await db.GroupPermissions
            .Where(gp => targetGroupIds.Contains(gp.GroupId))
            .Select(gp => gp.PermissionId)
            .Distinct()
            .ToListAsync();

        if (!targetPermissionIds.All(operatorPermissionIds.Contains))
        {
            throw new BusinessException("無法管理權限高於自己的使用者", StatusCodes.Status403Forbidden);
        }
    }

    /// <summary>
    /// 群組去除重複並確認存在，且不可授予高於自身的權限
    /// </summary>
    private async Task<List<int>> ValidateAssignableGroupsAsync(IEnumerable<int> groupIds, int operatorId)
    {
        var ids = groupIds.Distinct().ToList();
        if (ids.Count == 0) return ids;

        var groups = await db.Groups.AsNoTracking()
            .Where(g => ids.Contains(g.Id))
            .Select(g => new { g.Id, PermissionIds = g.GroupPermissions.Select(gp => gp.PermissionId).ToList() })
            .ToListAsync();

        if (groups.Count != ids.Count) throw new BusinessException("群組不存在");

        var operatorPermissionIds = await permissionService.GetUserPermissionIdsAsync(operatorId);
        var operatorIsAdmin = await permissionService.IsAdminAsync(operatorId);

        if (groups.Any(g => !CanAssignGroup(g.Id, g.PermissionIds, operatorPermissionIds, operatorIsAdmin)))
        {
            throw new BusinessException("不可指派權限高於自己的群組", StatusCodes.Status403Forbidden);
        }

        return ids;
    }

    private static bool CanAssignGroup(int groupId, List<int> groupPermissionIds, HashSet<int> operatorPermissionIds, bool operatorIsAdmin)
    {
        if (operatorIsAdmin) return true;
        if (groupId == SeedData.AdminGroupId) return false;
        return groupPermissionIds.All(operatorPermissionIds.Contains);
    }

    private async Task EnsureOtherActiveAdminAsync(int excludeUserId)
    {
        var hasOther = await db.UserGroups.AnyAsync(ug =>
            ug.GroupId == SeedData.AdminGroupId && ug.UserId != excludeUserId && ug.User.IsActive);
        if (!hasOther)
        {
            throw new BusinessException("系統管理員群組至少須保留一位啟用中的成員");
        }
    }
}

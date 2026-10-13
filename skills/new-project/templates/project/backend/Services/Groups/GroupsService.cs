using backend.Data;
using backend.DTOs;
using backend.DTOs.Groups;
using backend.Exceptions;
using backend.Models;
using backend.Services.Permissions;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Groups;

public class GroupsService(
    AppDbContext db,
    IPermissionService permissionService,
    ILogger<GroupsService> logger) : IGroupsService
{
    private const string NotFoundMessage = "群組不存在";

    public async Task<PagedResult<GroupListItem>> GetPagedAsync(PagedQuery query)
    {
        var groups = db.Groups.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            groups = groups.Where(g => g.Name.Contains(keyword) || (g.Description != null && g.Description.Contains(keyword)));
        }

        var totalCount = await groups.CountAsync();
        var items = await groups
            .OrderBy(g => g.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(g => new GroupListItem
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                MemberCount = g.UserGroups.Count,
                PermissionCount = g.GroupPermissions.Count,
                IsProtected = g.Id == SeedData.AdminGroupId,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<GroupListItem> { Items = items, TotalCount = totalCount, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<GroupDetail> GetAsync(int id)
    {
        return await db.Groups.AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new GroupDetail
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                IsProtected = g.Id == SeedData.AdminGroupId,
                PermissionIds = g.GroupPermissions.Select(gp => gp.PermissionId).ToList(),
                CreatedAt = g.CreatedAt
            })
            .FirstOrDefaultAsync()
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);
    }

    public async Task<List<PermissionItem>> GetPermissionsAsync(int operatorId)
    {
        var operatorPermissionIds = await permissionService.GetUserPermissionIdsAsync(operatorId);

        var permissions = await db.Permissions.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
        return permissions.Select(p => new PermissionItem
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Module = p.Module,
            Grantable = operatorPermissionIds.Contains(p.Id)
        }).ToList();
    }

    public async Task<int> CreateAsync(SaveGroupRequest request, int operatorId)
    {
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(name, null);
        var permissionIds = await ValidateGrantablePermissionsAsync(request.PermissionIds, operatorId);

        var group = new Group
        {
            Name = name,
            Description = NormalizeDescription(request.Description),
            CreatedAt = DateTime.Now
        };
        foreach (var permissionId in permissionIds)
        {
            group.GroupPermissions.Add(new GroupPermission { PermissionId = permissionId });
        }

        db.Groups.Add(group);
        await db.SaveChangesAsync();

        logger.LogInformation("新增群組：{GroupId}，操作者：{OperatorId}", group.Id, operatorId);
        return group.Id;
    }

    public async Task UpdateAsync(int id, SaveGroupRequest request, int operatorId)
    {
        var group = await db.Groups.Include(g => g.GroupPermissions).FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        await EnsureCanManageAsync(group, operatorId);

        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(name, id);

        var currentPermissionIds = group.GroupPermissions.Select(gp => gp.PermissionId).ToHashSet();
        var newPermissionIds = request.PermissionIds.Distinct().ToHashSet();
        var permissionsChanged = !currentPermissionIds.SetEquals(newPermissionIds);

        if (permissionsChanged)
        {
            if (id == SeedData.AdminGroupId)
            {
                throw new BusinessException("系統管理員群組的權限不可修改");
            }

            await ValidateGrantablePermissionsAsync(newPermissionIds, operatorId);

            foreach (var groupPermission in group.GroupPermissions.Where(gp => !newPermissionIds.Contains(gp.PermissionId)).ToList())
            {
                group.GroupPermissions.Remove(groupPermission);
            }
            foreach (var permissionId in newPermissionIds.Except(currentPermissionIds))
            {
                group.GroupPermissions.Add(new GroupPermission { GroupId = id, PermissionId = permissionId });
            }
        }

        group.Name = name;
        group.Description = NormalizeDescription(request.Description);

        await db.SaveChangesAsync();
        logger.LogInformation("編輯群組：{GroupId}，操作者：{OperatorId}", id, operatorId);
    }

    public async Task DeleteAsync(int id, int operatorId)
    {
        if (id == SeedData.AdminGroupId)
        {
            throw new BusinessException("系統管理員群組不可刪除");
        }

        var group = await db.Groups.Include(g => g.GroupPermissions).FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        await EnsureCanManageAsync(group, operatorId);

        db.Groups.Remove(group);
        await db.SaveChangesAsync();
        logger.LogInformation("刪除群組：{GroupId}，操作者：{OperatorId}", id, operatorId);
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludeId)
    {
        if (await db.Groups.AnyAsync(g => g.Name == name && g.Id != excludeId))
        {
            throw new BusinessException("群組名稱已存在");
        }
    }

    /// <summary>
    /// 系統管理員群組只能由系統管理員管理；不可管理權限高於自己的群組
    /// </summary>
    private async Task EnsureCanManageAsync(Group group, int operatorId)
    {
        if (await permissionService.IsAdminAsync(operatorId)) return;

        if (group.Id == SeedData.AdminGroupId)
        {
            throw new BusinessException("無法管理系統管理員群組", StatusCodes.Status403Forbidden);
        }

        var operatorPermissionIds = await permissionService.GetUserPermissionIdsAsync(operatorId);
        if (!group.GroupPermissions.All(gp => operatorPermissionIds.Contains(gp.PermissionId)))
        {
            throw new BusinessException("無法管理權限高於自己的群組", StatusCodes.Status403Forbidden);
        }
    }

    /// <summary>
    /// 權限去除重複並確認存在，且只能授予自己擁有的權限
    /// </summary>
    private async Task<List<int>> ValidateGrantablePermissionsAsync(IEnumerable<int> permissionIds, int operatorId)
    {
        var ids = permissionIds.Distinct().ToList();
        if (ids.Count == 0) return ids;

        var existingCount = await db.Permissions.CountAsync(p => ids.Contains(p.Id));
        if (existingCount != ids.Count) throw new BusinessException("權限不存在");

        var operatorPermissionIds = await permissionService.GetUserPermissionIdsAsync(operatorId);
        if (!ids.All(operatorPermissionIds.Contains))
        {
            throw new BusinessException("不可授予自己沒有的權限", StatusCodes.Status403Forbidden);
        }

        return ids;
    }

    private static string? NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}

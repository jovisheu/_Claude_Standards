using backend.Data;
using backend.DTOs;
using backend.DTOs.BasicGroups;
using backend.Exceptions;
using backend.Models;
using backend.Services.BasicPermissions;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.BasicGroups;

/// <summary>
/// 群組管理：系統管理員群組受保護（不可刪除、權限不可修改，且只有系統管理員能編輯）；不可授予或管理超過操作者權限的群組；
/// 群組仍有成員時不可刪除
/// </summary>
public class BasicGroupsService(
    AppDbContext db,
    IBasicPermissionsService basicPermissionsService,
    ILogger<BasicGroupsService> logger) : IBasicGroupsService
{
    private const string NotFoundMessage = "找不到指定的群組";

    public async Task<PagedResult<BasicGroupListItem>> GetPagedAsync(PagedQuery query, int operatorId)
    {
        var source = db.BasicGroup.AsNoTracking();
        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            source = source.Where(g => g.Name.Contains(keyword) || (g.Description != null && g.Description.Contains(keyword)));
        }

        var totalCount = await source.CountAsync();
        var rows = await source
            .OrderBy(g => g.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.CreatedAt,
                g.UpdatedAt,
                BasicUserCount = g.BasicUserGroup.Count,
                BasicPermissionIds = g.BasicGroupPermission.Select(gp => gp.BasicPermissionId).ToList(),
            })
            .ToListAsync();

        var operatorPermissionIds = await basicPermissionsService.GetBasicPermissionIdsAsync(operatorId);
        var isAdmin = await basicPermissionsService.IsAdminAsync(operatorId);
        return new PagedResult<BasicGroupListItem>
        {
            Items = rows.Select(r => new BasicGroupListItem
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsProtected = IsProtected(r.Id),
                IsManageable = CanManage(r.Id, r.BasicPermissionIds, operatorPermissionIds, isAdmin),
                BasicUserCount = r.BasicUserCount,
                BasicPermissionCount = r.BasicPermissionIds.Count,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            }).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<BasicGroupDetail> GetAsync(int id, int operatorId)
    {
        var basicGroup = await db.BasicGroup.AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.CreatedAt,
                g.UpdatedAt,
                BasicPermissionIds = g.BasicGroupPermission.Select(gp => gp.BasicPermissionId).OrderBy(x => x).ToList(),
            })
            .FirstOrDefaultAsync()
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        var operatorPermissionIds = await basicPermissionsService.GetBasicPermissionIdsAsync(operatorId);
        var isAdmin = await basicPermissionsService.IsAdminAsync(operatorId);
        return new BasicGroupDetail
        {
            Id = basicGroup.Id,
            Name = basicGroup.Name,
            Description = basicGroup.Description,
            IsProtected = IsProtected(basicGroup.Id),
            IsManageable = CanManage(basicGroup.Id, basicGroup.BasicPermissionIds, operatorPermissionIds, isAdmin),
            BasicPermissionIds = basicGroup.BasicPermissionIds,
            CreatedAt = basicGroup.CreatedAt,
            UpdatedAt = basicGroup.UpdatedAt,
        };
    }

    public async Task<List<BasicPermissionItem>> GetBasicPermissionOptionsAsync(int operatorId)
    {
        var operatorPermissionIds = await basicPermissionsService.GetBasicPermissionIdsAsync(operatorId);
        var permissions = await db.BasicPermission.AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new BasicPermissionItem { Id = p.Id, Name = p.Name, Description = p.Description, Module = p.Module })
            .ToListAsync();
        foreach (var permission in permissions)
        {
            permission.IsAssignable = operatorPermissionIds.Contains(permission.Id);
        }

        return permissions;
    }

    public async Task<int> CreateAsync(BasicGroupSaveRequest request, int operatorId)
    {
        var name = request.Name.Trim();
        if (await db.BasicGroup.AnyAsync(g => g.Name == name))
        {
            throw new BusinessException("群組名稱已存在");
        }

        var basicPermissionIds = request.BasicPermissionIds.Distinct().ToHashSet();
        await EnsurePermissionsAsync(basicPermissionIds, basicPermissionIds, operatorId);

        var basicGroup = new BasicGroup
        {
            Name = name,
            Description = NormalizeDescription(request.Description),
            CreatedAt = DateTime.Now,
        };
        foreach (var basicPermissionId in basicPermissionIds)
        {
            basicGroup.BasicGroupPermission.Add(new BasicGroupPermission { BasicPermissionId = basicPermissionId });
        }

        db.BasicGroup.Add(basicGroup);
        await db.SaveChangesAsync();
        logger.LogInformation("新增群組：{BasicGroupId}，操作者：{OperatorId}", basicGroup.Id, operatorId);
        return basicGroup.Id;
    }

    public async Task UpdateAsync(int id, BasicGroupSaveRequest request, int operatorId)
    {
        var basicGroup = await db.BasicGroup.Include(g => g.BasicGroupPermission).FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        var currentPermissionIds = basicGroup.BasicGroupPermission.Select(gp => gp.BasicPermissionId).ToHashSet();
        await EnsureManageableAsync(id, currentPermissionIds, operatorId);

        var name = request.Name.Trim();
        if (await db.BasicGroup.AnyAsync(g => g.Name == name && g.Id != id))
        {
            throw new BusinessException("群組名稱已存在");
        }

        var newPermissionIds = request.BasicPermissionIds.Distinct().ToHashSet();
        if (IsProtected(id) && !newPermissionIds.SetEquals(currentPermissionIds))
        {
            throw new BusinessException("系統管理員群組的權限不可修改", StatusCodes.Status403Forbidden);
        }

        // 新增或移除的權限都須在操作者權限內
        var changed = newPermissionIds.Except(currentPermissionIds).Union(currentPermissionIds.Except(newPermissionIds)).ToHashSet();
        await EnsurePermissionsAsync(newPermissionIds, changed, operatorId);

        basicGroup.Name = name;
        basicGroup.Description = NormalizeDescription(request.Description);
        basicGroup.UpdatedAt = DateTime.Now;

        foreach (var groupPermission in basicGroup.BasicGroupPermission.Where(gp => !newPermissionIds.Contains(gp.BasicPermissionId)).ToList())
        {
            basicGroup.BasicGroupPermission.Remove(groupPermission);
        }

        foreach (var basicPermissionId in newPermissionIds.Except(currentPermissionIds))
        {
            basicGroup.BasicGroupPermission.Add(new BasicGroupPermission { BasicGroupId = id, BasicPermissionId = basicPermissionId });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("編輯群組：{BasicGroupId}，操作者：{OperatorId}", id, operatorId);
    }

    public async Task DeleteAsync(int id, int operatorId)
    {
        if (IsProtected(id))
        {
            throw new BusinessException("系統管理員群組不可刪除", StatusCodes.Status403Forbidden);
        }

        var basicGroup = await db.BasicGroup.Include(g => g.BasicGroupPermission).FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        await EnsureManageableAsync(id, basicGroup.BasicGroupPermission.Select(gp => gp.BasicPermissionId), operatorId);

        if (await db.BasicUserGroup.AnyAsync(ug => ug.BasicGroupId == id))
        {
            throw new BusinessException("群組仍有成員，請先將成員移出群組再刪除");
        }

        db.BasicGroupPermission.RemoveRange(basicGroup.BasicGroupPermission);
        db.BasicGroup.Remove(basicGroup);
        await db.SaveChangesAsync();
        logger.LogInformation("刪除群組：{BasicGroupId}，操作者：{OperatorId}", id, operatorId);
    }

    private static bool IsProtected(int basicGroupId) => basicGroupId == SeedData.AdminBasicGroupId;

    private static string? NormalizeDescription(string? description)
    {
        var text = description?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>系統管理員群組只有系統管理員能管理；其他群組的權限須都在操作者權限內</summary>
    private static bool CanManage(int basicGroupId, IEnumerable<int> basicPermissionIds, IReadOnlySet<int> operatorPermissionIds, bool isAdmin)
        => (!IsProtected(basicGroupId) || isAdmin) && basicPermissionIds.All(operatorPermissionIds.Contains);

    private async Task EnsureManageableAsync(int basicGroupId, IEnumerable<int> basicPermissionIds, int operatorId)
    {
        var operatorPermissionIds = await basicPermissionsService.GetBasicPermissionIdsAsync(operatorId);
        var isAdmin = await basicPermissionsService.IsAdminAsync(operatorId);
        if (!CanManage(basicGroupId, basicPermissionIds, operatorPermissionIds, isAdmin))
        {
            throw new BusinessException("此群組的權限超過您的權限，無法管理", StatusCodes.Status403Forbidden);
        }
    }

    /// <summary>權限須存在；異動的權限須為操作者擁有的權限</summary>
    private async Task EnsurePermissionsAsync(HashSet<int> basicPermissionIds, HashSet<int> changedPermissionIds, int operatorId)
    {
        var existingCount = await db.BasicPermission.CountAsync(p => basicPermissionIds.Contains(p.Id));
        if (existingCount != basicPermissionIds.Count)
        {
            throw new BusinessException("指定的權限不存在");
        }

        var operatorPermissionIds = await basicPermissionsService.GetBasicPermissionIdsAsync(operatorId);
        if (!changedPermissionIds.All(operatorPermissionIds.Contains))
        {
            throw new BusinessException("不可授予或移除超過自身權限的權限", StatusCodes.Status403Forbidden);
        }
    }
}

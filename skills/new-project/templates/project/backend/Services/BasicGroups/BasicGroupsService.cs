using backend.Data;
using backend.DTOs;
using backend.DTOs.BasicGroups;
using backend.Exceptions;
using backend.Models;
using backend.Services.BasicPermissions;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.BasicGroups;

public class BasicGroupsService(
    AppDbContext db,
    IBasicPermissionsService basicPermissionsService,
    ILogger<BasicGroupsService> logger) : IBasicGroupsService
{
    private const string NotFoundMessage = "群組不存在";

    public async Task<PagedResult<BasicGroupListItem>> GetPagedAsync(PagedQuery query)
    {
        var basicGroups = db.BasicGroup.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            basicGroups = basicGroups.Where(g => g.Name.Contains(keyword) || (g.Description != null && g.Description.Contains(keyword)));
        }

        var totalCount = await basicGroups.CountAsync();
        var items = await basicGroups
            .OrderBy(g => g.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(g => new BasicGroupListItem
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                BasicUserCount = g.BasicUserGroup.Count,
                BasicPermissionCount = g.BasicGroupPermission.Count,
                IsProtected = g.Id == SeedData.AdminBasicGroupId,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            })
            .ToListAsync();

        return new PagedResult<BasicGroupListItem> { Items = items, TotalCount = totalCount, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<BasicGroupDetail> GetAsync(int id)
    {
        return await db.BasicGroup.AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new BasicGroupDetail
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                IsProtected = g.Id == SeedData.AdminBasicGroupId,
                BasicPermissionIds = g.BasicGroupPermission.Select(gp => gp.BasicPermissionId).ToList(),
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            })
            .FirstOrDefaultAsync()
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);
    }

    public async Task<List<BasicPermissionItem>> GetBasicPermissionsAsync(int operatorId)
    {
        var operatorBasicPermissionIds = await basicPermissionsService.GetBasicUserPermissionIdsAsync(operatorId);

        var basicPermissions = await db.BasicPermission.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
        return basicPermissions.Select(p => new BasicPermissionItem
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Module = p.Module,
            Grantable = operatorBasicPermissionIds.Contains(p.Id)
        }).ToList();
    }

    public async Task<int> CreateAsync(BasicGroupSaveRequest request, int operatorId)
    {
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(name, null);
        var basicPermissionIds = await ValidateGrantableBasicPermissionsAsync(request.BasicPermissionIds, operatorId);

        var basicGroup = new BasicGroup
        {
            Name = name,
            Description = NormalizeDescription(request.Description),
            CreatedAt = DateTime.Now
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

        await EnsureCanManageAsync(basicGroup, operatorId);

        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(name, id);

        var currentBasicPermissionIds = basicGroup.BasicGroupPermission.Select(gp => gp.BasicPermissionId).ToHashSet();
        var newBasicPermissionIds = request.BasicPermissionIds.Distinct().ToHashSet();
        var basicPermissionsChanged = !currentBasicPermissionIds.SetEquals(newBasicPermissionIds);

        if (basicPermissionsChanged)
        {
            if (id == SeedData.AdminBasicGroupId)
            {
                throw new BusinessException("系統管理員群組的權限不可修改");
            }

            await ValidateGrantableBasicPermissionsAsync(newBasicPermissionIds, operatorId);

            foreach (var basicGroupPermission in basicGroup.BasicGroupPermission.Where(gp => !newBasicPermissionIds.Contains(gp.BasicPermissionId)).ToList())
            {
                basicGroup.BasicGroupPermission.Remove(basicGroupPermission);
            }
            foreach (var basicPermissionId in newBasicPermissionIds.Except(currentBasicPermissionIds))
            {
                basicGroup.BasicGroupPermission.Add(new BasicGroupPermission { BasicGroupId = id, BasicPermissionId = basicPermissionId });
            }
        }

        basicGroup.Name = name;
        basicGroup.Description = NormalizeDescription(request.Description);
        basicGroup.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        logger.LogInformation("編輯群組：{BasicGroupId}，操作者：{OperatorId}", id, operatorId);
    }

    public async Task DeleteAsync(int id, int operatorId)
    {
        if (id == SeedData.AdminBasicGroupId)
        {
            throw new BusinessException("系統管理員群組不可刪除");
        }

        var basicGroup = await db.BasicGroup.Include(g => g.BasicGroupPermission).FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new BusinessException(NotFoundMessage, StatusCodes.Status404NotFound);

        await EnsureCanManageAsync(basicGroup, operatorId);

        db.BasicGroup.Remove(basicGroup);
        await db.SaveChangesAsync();
        logger.LogInformation("刪除群組：{BasicGroupId}，操作者：{OperatorId}", id, operatorId);
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludeId)
    {
        if (await db.BasicGroup.AnyAsync(g => g.Name == name && g.Id != excludeId))
        {
            throw new BusinessException("群組名稱已存在");
        }
    }

    /// <summary>
    /// 系統管理員群組只能由系統管理員管理；不可管理權限高於自己的群組
    /// </summary>
    private async Task EnsureCanManageAsync(BasicGroup basicGroup, int operatorId)
    {
        if (await basicPermissionsService.IsAdminAsync(operatorId)) return;

        if (basicGroup.Id == SeedData.AdminBasicGroupId)
        {
            throw new BusinessException("無法管理系統管理員群組", StatusCodes.Status403Forbidden);
        }

        var operatorBasicPermissionIds = await basicPermissionsService.GetBasicUserPermissionIdsAsync(operatorId);
        if (!basicGroup.BasicGroupPermission.All(gp => operatorBasicPermissionIds.Contains(gp.BasicPermissionId)))
        {
            throw new BusinessException("無法管理權限高於自己的群組", StatusCodes.Status403Forbidden);
        }
    }

    /// <summary>
    /// 權限去除重複並確認存在，且只能授予自己擁有的權限
    /// </summary>
    private async Task<List<int>> ValidateGrantableBasicPermissionsAsync(IEnumerable<int> basicPermissionIds, int operatorId)
    {
        var ids = basicPermissionIds.Distinct().ToList();
        if (ids.Count == 0) return ids;

        var existingCount = await db.BasicPermission.CountAsync(p => ids.Contains(p.Id));
        if (existingCount != ids.Count) throw new BusinessException("權限不存在");

        var operatorBasicPermissionIds = await basicPermissionsService.GetBasicUserPermissionIdsAsync(operatorId);
        if (!ids.All(operatorBasicPermissionIds.Contains))
        {
            throw new BusinessException("不可授予自己沒有的權限", StatusCodes.Status403Forbidden);
        }

        return ids;
    }

    private static string? NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}

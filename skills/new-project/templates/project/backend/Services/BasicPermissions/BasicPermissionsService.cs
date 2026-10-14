using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.BasicPermissions;

/// <summary>
/// 權限一律即時查資料庫；同一請求內快取查詢結果
/// </summary>
public class BasicPermissionsService(AppDbContext db) : IBasicPermissionsService
{
    private readonly Dictionary<int, List<(int Id, string Name)>> _cache = [];

    public async Task<HashSet<string>> GetBasicUserPermissionsAsync(int basicUserId)
        => (await LoadAsync(basicUserId)).Select(p => p.Name).ToHashSet();

    public async Task<HashSet<int>> GetBasicUserPermissionIdsAsync(int basicUserId)
        => (await LoadAsync(basicUserId)).Select(p => p.Id).ToHashSet();

    public Task<bool> IsAdminAsync(int basicUserId)
        => db.BasicUserGroup.AnyAsync(ug => ug.BasicUserId == basicUserId && ug.BasicGroupId == SeedData.AdminBasicGroupId && ug.BasicUser.IsActive);

    private async Task<List<(int Id, string Name)>> LoadAsync(int basicUserId)
    {
        if (_cache.TryGetValue(basicUserId, out var cached)) return cached;

        var rows = await db.BasicUserGroup
            .Where(ug => ug.BasicUserId == basicUserId && ug.BasicUser.IsActive)
            .SelectMany(ug => ug.BasicGroup.BasicGroupPermission)
            .Select(gp => new { gp.BasicPermission.Id, gp.BasicPermission.Name })
            .Distinct()
            .ToListAsync();

        var result = rows.Select(r => (r.Id, r.Name)).ToList();
        _cache[basicUserId] = result;
        return result;
    }
}

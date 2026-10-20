using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.BasicPermissions;

/// <summary>判斷操作者權限一律查資料庫（同一請求內快取查詢結果）</summary>
public class BasicPermissionsService(AppDbContext db) : IBasicPermissionsService
{
    private readonly Dictionary<int, List<(int Id, string Name)>?> _cache = [];

    public async Task<IReadOnlySet<string>?> GetBasicPermissionNamesAsync(int basicUserId)
    {
        var permissions = await LoadAsync(basicUserId);
        return permissions?.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
    }

    public async Task<IReadOnlySet<int>> GetBasicPermissionIdsAsync(int basicUserId)
    {
        var permissions = await LoadAsync(basicUserId);
        return permissions?.Select(p => p.Id).ToHashSet() ?? [];
    }

    public Task<bool> IsAdminAsync(int basicUserId)
        => db.BasicUserGroup.AsNoTracking().AnyAsync(ug =>
            ug.BasicUserId == basicUserId && ug.BasicGroupId == SeedData.AdminBasicGroupId && ug.BasicUser!.IsActive);

    private async Task<List<(int Id, string Name)>?> LoadAsync(int basicUserId)
    {
        if (_cache.TryGetValue(basicUserId, out var cached))
        {
            return cached;
        }

        var isActive = await db.BasicUser.AsNoTracking().AnyAsync(u => u.Id == basicUserId && u.IsActive);
        List<(int, string)>? result = null;
        if (isActive)
        {
            var rows = await db.BasicUserGroup.AsNoTracking()
                .Where(ug => ug.BasicUserId == basicUserId)
                .SelectMany(ug => ug.BasicGroup!.BasicGroupPermission)
                .Select(gp => new { gp.BasicPermission!.Id, gp.BasicPermission.Name })
                .Distinct()
                .ToListAsync();
            result = rows.Select(r => (r.Id, r.Name)).ToList();
        }

        _cache[basicUserId] = result;
        return result;
    }
}

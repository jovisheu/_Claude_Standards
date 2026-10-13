using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Permissions;

/// <summary>
/// 權限一律即時查資料庫；同一請求內快取查詢結果
/// </summary>
public class PermissionService(AppDbContext db) : IPermissionService
{
    private readonly Dictionary<int, List<(int Id, string Name)>> _cache = [];

    public async Task<HashSet<string>> GetUserPermissionsAsync(int userId)
        => (await LoadAsync(userId)).Select(p => p.Name).ToHashSet();

    public async Task<HashSet<int>> GetUserPermissionIdsAsync(int userId)
        => (await LoadAsync(userId)).Select(p => p.Id).ToHashSet();

    public Task<bool> IsAdminAsync(int userId)
        => db.UserGroups.AnyAsync(ug => ug.UserId == userId && ug.GroupId == SeedData.AdminGroupId && ug.User.IsActive);

    private async Task<List<(int Id, string Name)>> LoadAsync(int userId)
    {
        if (_cache.TryGetValue(userId, out var cached)) return cached;

        var rows = await db.UserGroups
            .Where(ug => ug.UserId == userId && ug.User.IsActive)
            .SelectMany(ug => ug.Group.GroupPermissions)
            .Select(gp => new { gp.Permission.Id, gp.Permission.Name })
            .Distinct()
            .ToListAsync();

        var result = rows.Select(r => (r.Id, r.Name)).ToList();
        _cache[userId] = result;
        return result;
    }
}

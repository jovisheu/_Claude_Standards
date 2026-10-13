using backend.DTOs;
using backend.DTOs.Groups;

namespace backend.Services.Groups;

public interface IGroupsService
{
    Task<PagedResult<GroupListItem>> GetPagedAsync(PagedQuery query);
    Task<GroupDetail> GetAsync(int id);
    Task<List<PermissionItem>> GetPermissionsAsync(int operatorId);
    Task<int> CreateAsync(SaveGroupRequest request, int operatorId);
    Task UpdateAsync(int id, SaveGroupRequest request, int operatorId);
    Task DeleteAsync(int id, int operatorId);
}

using backend.DTOs;
using backend.DTOs.BasicGroups;

namespace backend.Services.BasicGroups;

public interface IBasicGroupsService
{
    Task<PagedResult<BasicGroupListItem>> GetPagedAsync(PagedQuery query);
    Task<BasicGroupDetail> GetAsync(int id);
    Task<List<BasicPermissionItem>> GetBasicPermissionsAsync(int operatorId);
    Task<int> CreateAsync(BasicGroupSaveRequest request, int operatorId);
    Task UpdateAsync(int id, BasicGroupSaveRequest request, int operatorId);
    Task DeleteAsync(int id, int operatorId);
}

using backend.DTOs;
using backend.DTOs.BasicGroups;

namespace backend.Services.BasicGroups;

public interface IBasicGroupsService
{
    Task<PagedResult<BasicGroupListItem>> GetPagedAsync(PagedQuery query, int operatorId);

    Task<BasicGroupDetail> GetAsync(int id, int operatorId);

    Task<List<BasicPermissionItem>> GetBasicPermissionOptionsAsync(int operatorId);

    Task<int> CreateAsync(BasicGroupSaveRequest request, int operatorId);

    Task UpdateAsync(int id, BasicGroupSaveRequest request, int operatorId);

    Task DeleteAsync(int id, int operatorId);
}

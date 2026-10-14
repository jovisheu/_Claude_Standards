using backend.DTOs;
using backend.DTOs.BasicUsers;

namespace backend.Services.BasicUsers;

public interface IBasicUsersService
{
    Task<PagedResult<BasicUserListItem>> GetPagedAsync(PagedQuery query);
    Task<BasicUserDetail> GetAsync(int id);
    Task<List<BasicGroupOption>> GetBasicGroupOptionsAsync(int operatorId);
    Task<int> CreateAsync(BasicUserCreateRequest request, int operatorId);
    Task UpdateAsync(int id, BasicUserUpdateRequest request, int operatorId);
    Task DeleteAsync(int id, int operatorId);
}

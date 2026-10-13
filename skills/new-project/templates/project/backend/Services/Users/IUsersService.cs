using backend.DTOs;
using backend.DTOs.Users;

namespace backend.Services.Users;

public interface IUsersService
{
    Task<PagedResult<UserListItem>> GetPagedAsync(PagedQuery query);
    Task<UserDetail> GetAsync(int id);
    Task<List<GroupOption>> GetGroupOptionsAsync(int operatorId);
    Task<int> CreateAsync(CreateUserRequest request, int operatorId);
    Task UpdateAsync(int id, UpdateUserRequest request, int operatorId);
    Task DeleteAsync(int id, int operatorId);
}

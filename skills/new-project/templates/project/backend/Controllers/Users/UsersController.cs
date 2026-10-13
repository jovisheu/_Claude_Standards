using backend.Data;
using backend.DTOs;
using backend.DTOs.Users;
using backend.Extensions;
using backend.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Users;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IUsersService usersService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionNames.UsersRead)]
    public async Task<ApiResponse<List<UserListItem>>> GetPaged([FromQuery] PagedQuery query)
        => ApiResponse.Paged(await usersService.GetPagedAsync(query));

    [HttpGet("{id:int}")]
    [Authorize(Policy = PermissionNames.UsersRead)]
    public async Task<ApiResponse<UserDetail>> Get(int id)
        => ApiResponse<UserDetail>.Ok(await usersService.GetAsync(id));

    /// <summary>使用者表單的群組選項</summary>
    [HttpGet("group-options")]
    [Authorize(Policy = PermissionNames.UsersRead)]
    public async Task<ApiResponse<List<GroupOption>>> GetGroupOptions()
        => ApiResponse<List<GroupOption>>.Ok(await usersService.GetGroupOptionsAsync(User.GetUserId()));

    [HttpPost]
    [Authorize(Policy = PermissionNames.UsersCreate)]
    public async Task<ApiResponse<int>> Create(CreateUserRequest request)
        => ApiResponse<int>.Ok(await usersService.CreateAsync(request, User.GetUserId()), "新增成功");

    [HttpPut("{id:int}")]
    [Authorize(Policy = PermissionNames.UsersUpdate)]
    public async Task<ApiResponse> Update(int id, UpdateUserRequest request)
    {
        await usersService.UpdateAsync(id, request, User.GetUserId());
        return ApiResponse.Success("儲存成功");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PermissionNames.UsersDelete)]
    public async Task<ApiResponse> Delete(int id)
    {
        await usersService.DeleteAsync(id, User.GetUserId());
        return ApiResponse.Success("刪除成功");
    }
}

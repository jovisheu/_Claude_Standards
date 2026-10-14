using backend.Data;
using backend.DTOs;
using backend.DTOs.BasicUsers;
using backend.Extensions;
using backend.Services.BasicUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.BasicUsers;

[ApiController]
[Route("api/basicusers")]
[Authorize]
public class BasicUsersController(IBasicUsersService basicUsersService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = BasicPermissionNames.BasicUserRead)]
    public async Task<ApiResponse<List<BasicUserListItem>>> GetPaged([FromQuery] PagedQuery query)
        => ApiResponse.Paged(await basicUsersService.GetPagedAsync(query));

    [HttpGet("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicUserRead)]
    public async Task<ApiResponse<BasicUserDetail>> Get(int id)
        => ApiResponse<BasicUserDetail>.Ok(await basicUsersService.GetAsync(id));

    /// <summary>使用者表單的群組選項</summary>
    [HttpGet("basicgroup-options")]
    [Authorize(Policy = BasicPermissionNames.BasicUserRead)]
    public async Task<ApiResponse<List<BasicGroupOption>>> GetBasicGroupOptions()
        => ApiResponse<List<BasicGroupOption>>.Ok(await basicUsersService.GetBasicGroupOptionsAsync(User.GetBasicUserId()));

    [HttpPost]
    [Authorize(Policy = BasicPermissionNames.BasicUserCreate)]
    public async Task<ApiResponse<int>> Create(BasicUserCreateRequest request)
        => ApiResponse<int>.Ok(await basicUsersService.CreateAsync(request, User.GetBasicUserId()), "新增成功");

    [HttpPut("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicUserUpdate)]
    public async Task<ApiResponse> Update(int id, BasicUserUpdateRequest request)
    {
        await basicUsersService.UpdateAsync(id, request, User.GetBasicUserId());
        return ApiResponse.Success("儲存成功");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicUserDelete)]
    public async Task<ApiResponse> Delete(int id)
    {
        await basicUsersService.DeleteAsync(id, User.GetBasicUserId());
        return ApiResponse.Success("刪除成功");
    }
}

using backend.Data;
using backend.DTOs;
using backend.DTOs.Groups;
using backend.Extensions;
using backend.Services.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Groups;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController(IGroupsService groupsService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionNames.GroupsRead)]
    public async Task<ApiResponse<List<GroupListItem>>> GetPaged([FromQuery] PagedQuery query)
        => ApiResponse.Paged(await groupsService.GetPagedAsync(query));

    [HttpGet("{id:int}")]
    [Authorize(Policy = PermissionNames.GroupsRead)]
    public async Task<ApiResponse<GroupDetail>> Get(int id)
        => ApiResponse<GroupDetail>.Ok(await groupsService.GetAsync(id));

    /// <summary>全部權限（標示操作者可授予者）</summary>
    [HttpGet("permissions")]
    [Authorize(Policy = PermissionNames.GroupsRead)]
    public async Task<ApiResponse<List<PermissionItem>>> GetPermissions()
        => ApiResponse<List<PermissionItem>>.Ok(await groupsService.GetPermissionsAsync(User.GetUserId()));

    [HttpPost]
    [Authorize(Policy = PermissionNames.GroupsCreate)]
    public async Task<ApiResponse<int>> Create(SaveGroupRequest request)
        => ApiResponse<int>.Ok(await groupsService.CreateAsync(request, User.GetUserId()), "新增成功");

    [HttpPut("{id:int}")]
    [Authorize(Policy = PermissionNames.GroupsUpdate)]
    public async Task<ApiResponse> Update(int id, SaveGroupRequest request)
    {
        await groupsService.UpdateAsync(id, request, User.GetUserId());
        return ApiResponse.Success("儲存成功");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PermissionNames.GroupsDelete)]
    public async Task<ApiResponse> Delete(int id)
    {
        await groupsService.DeleteAsync(id, User.GetUserId());
        return ApiResponse.Success("刪除成功");
    }
}

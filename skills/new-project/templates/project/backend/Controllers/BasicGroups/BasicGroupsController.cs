using backend.Data;
using backend.DTOs;
using backend.DTOs.BasicGroups;
using backend.Extensions;
using backend.Services.BasicGroups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.BasicGroups;

[ApiController]
[Route("api/basicgroups")]
[Authorize]
public class BasicGroupsController(IBasicGroupsService basicGroupsService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = BasicPermissionNames.BasicGroupRead)]
    public async Task<ApiResponse<List<BasicGroupListItem>>> GetPaged([FromQuery] PagedQuery query)
        => ApiResponse.Paged(await basicGroupsService.GetPagedAsync(query));

    [HttpGet("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicGroupRead)]
    public async Task<ApiResponse<BasicGroupDetail>> Get(int id)
        => ApiResponse<BasicGroupDetail>.Ok(await basicGroupsService.GetAsync(id));

    /// <summary>全部權限（標示操作者可授予者）</summary>
    [HttpGet("basicpermissions")]
    [Authorize(Policy = BasicPermissionNames.BasicGroupRead)]
    public async Task<ApiResponse<List<BasicPermissionItem>>> GetBasicPermissions()
        => ApiResponse<List<BasicPermissionItem>>.Ok(await basicGroupsService.GetBasicPermissionsAsync(User.GetBasicUserId()));

    [HttpPost]
    [Authorize(Policy = BasicPermissionNames.BasicGroupCreate)]
    public async Task<ApiResponse<int>> Create(BasicGroupSaveRequest request)
        => ApiResponse<int>.Ok(await basicGroupsService.CreateAsync(request, User.GetBasicUserId()), "新增成功");

    [HttpPut("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicGroupUpdate)]
    public async Task<ApiResponse> Update(int id, BasicGroupSaveRequest request)
    {
        await basicGroupsService.UpdateAsync(id, request, User.GetBasicUserId());
        return ApiResponse.Success("儲存成功");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicGroupDelete)]
    public async Task<ApiResponse> Delete(int id)
    {
        await basicGroupsService.DeleteAsync(id, User.GetBasicUserId());
        return ApiResponse.Success("刪除成功");
    }
}

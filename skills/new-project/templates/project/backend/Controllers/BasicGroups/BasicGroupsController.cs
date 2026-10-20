using backend.Data;
using backend.DTOs;
using backend.DTOs.BasicGroups;
using backend.DTOs.BasicSyslogs;
using backend.Extensions;
using backend.Models;
using backend.Services.BasicGroups;
using backend.Services.BasicSyslogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.BasicGroups;

[ApiController]
[Route("api/basicgroups")]
[Authorize]
public class BasicGroupsController(IBasicGroupsService basicGroupsService, IBasicSyslogHistoryService basicSyslogHistoryService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = BasicPermissionNames.BasicGroupRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BasicGroupListItem>>>> GetPaged([FromQuery] PagedQuery query)
        => Ok(ApiResponse.Paged(await basicGroupsService.GetPagedAsync(query, User.GetBasicUserId())));

    [HttpGet("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicGroupRead)]
    public async Task<ActionResult<ApiResponse<BasicGroupDetail>>> Get(int id)
        => Ok(ApiResponse<BasicGroupDetail>.Success(await basicGroupsService.GetAsync(id, User.GetBasicUserId())));

    [HttpGet("basicpermission-options")]
    [Authorize(Policy = BasicPermissionNames.BasicGroupRead)]
    public async Task<ActionResult<ApiResponse<List<BasicPermissionItem>>>> GetBasicPermissionOptions()
        => Ok(ApiResponse<List<BasicPermissionItem>>.Success(await basicGroupsService.GetBasicPermissionOptionsAsync(User.GetBasicUserId())));

    [HttpPost]
    [Authorize(Policy = BasicPermissionNames.BasicGroupCreate)]
    [BasicSyslogDescription("新增群組")]
    public async Task<ActionResult<ApiResponse<int>>> Create(BasicGroupSaveRequest request)
        => Ok(ApiResponse<int>.Success(await basicGroupsService.CreateAsync(request, User.GetBasicUserId()), "新增成功"));

    [HttpPut("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicGroupUpdate)]
    [BasicSyslogDescription("編輯群組")]
    public async Task<ActionResult<ApiResponse>> Update(int id, BasicGroupSaveRequest request)
    {
        await basicGroupsService.UpdateAsync(id, request, User.GetBasicUserId());
        return Ok(ApiResponse.Ok("儲存成功"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicGroupDelete)]
    [BasicSyslogDescription("刪除群組")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await basicGroupsService.DeleteAsync(id, User.GetBasicUserId());
        return Ok(ApiResponse.Ok("刪除成功"));
    }

    /// <summary>單筆資料的異動歷程（可編輯者才能檢視）</summary>
    [HttpGet("{id:int}/basicsyslogs")]
    [Authorize(Policy = BasicPermissionNames.BasicGroupUpdate)]
    public async Task<ActionResult<ApiResponse<BasicSyslogHistoryResult>>> GetBasicSyslogs(int id)
        => Ok(ApiResponse<BasicSyslogHistoryResult>.Success(
            await basicSyslogHistoryService.GetRecordHistoryAsync(nameof(BasicGroup), id, nameof(BasicGroupPermission), nameof(BasicUserGroup))));
}

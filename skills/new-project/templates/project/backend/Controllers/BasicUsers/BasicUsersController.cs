using backend.Data;
using backend.DTOs;
using backend.DTOs.BasicSyslogs;
using backend.DTOs.BasicUsers;
using backend.Extensions;
using backend.Models;
using backend.Services.BasicSyslogs;
using backend.Services.BasicUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.BasicUsers;

[ApiController]
[Route("api/basicusers")]
[Authorize]
public class BasicUsersController(IBasicUsersService basicUsersService, IBasicSyslogHistoryService basicSyslogHistoryService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = BasicPermissionNames.BasicUserRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BasicUserListItem>>>> GetPaged([FromQuery] PagedQuery query)
        => Ok(ApiResponse.Paged(await basicUsersService.GetPagedAsync(query, User.GetBasicUserId())));

    [HttpGet("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicUserRead)]
    public async Task<ActionResult<ApiResponse<BasicUserDetail>>> Get(int id)
        => Ok(ApiResponse<BasicUserDetail>.Success(await basicUsersService.GetAsync(id, User.GetBasicUserId())));

    [HttpGet("basicgroup-options")]
    [Authorize(Policy = BasicPermissionNames.BasicUserRead)]
    public async Task<ActionResult<ApiResponse<List<BasicGroupOption>>>> GetBasicGroupOptions()
        => Ok(ApiResponse<List<BasicGroupOption>>.Success(await basicUsersService.GetBasicGroupOptionsAsync(User.GetBasicUserId())));

    [HttpPost]
    [Authorize(Policy = BasicPermissionNames.BasicUserCreate)]
    [BasicSyslogDescription("新增使用者")]
    public async Task<ActionResult<ApiResponse<int>>> Create(BasicUserCreateRequest request)
        => Ok(ApiResponse<int>.Success(await basicUsersService.CreateAsync(request, User.GetBasicUserId()), "新增成功"));

    [HttpPut("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicUserUpdate)]
    [BasicSyslogDescription("編輯使用者")]
    public async Task<ActionResult<ApiResponse>> Update(int id, BasicUserUpdateRequest request)
    {
        await basicUsersService.UpdateAsync(id, request, User.GetBasicUserId());
        return Ok(ApiResponse.Ok("儲存成功"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = BasicPermissionNames.BasicUserDelete)]
    [BasicSyslogDescription("刪除使用者")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await basicUsersService.DeleteAsync(id, User.GetBasicUserId());
        return Ok(ApiResponse.Ok("刪除成功"));
    }

    /// <summary>單筆資料的異動歷程（可編輯者才能檢視）</summary>
    [HttpGet("{id:int}/basicsyslogs")]
    [Authorize(Policy = BasicPermissionNames.BasicUserUpdate)]
    public async Task<ActionResult<ApiResponse<BasicSyslogHistoryResult>>> GetBasicSyslogs(int id)
        => Ok(ApiResponse<BasicSyslogHistoryResult>.Success(
            await basicSyslogHistoryService.GetRecordHistoryAsync(nameof(BasicUser), id, nameof(BasicUserGroup))));
}

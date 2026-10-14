using backend.Services.BasicPermissions;
using Microsoft.AspNetCore.Authorization;

namespace backend.Extensions;

/// <summary>
/// 權限需求（Policy 名稱與權限名稱相同）
/// </summary>
public class BasicPermissionRequirement(string basicPermission) : IAuthorizationRequirement
{
    public string BasicPermission { get; } = basicPermission;
}

/// <summary>
/// 判斷操作者權限一律查資料庫，不信任 JWT 內可能已過時的權限
/// </summary>
public class BasicPermissionAuthorizationHandler(IBasicPermissionsService basicPermissionsService) : AuthorizationHandler<BasicPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BasicPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        var basicUserId = context.User.GetBasicUserId();
        var basicPermissions = await basicPermissionsService.GetBasicUserPermissionsAsync(basicUserId);
        if (basicPermissions.Contains(requirement.BasicPermission))
        {
            context.Succeed(requirement);
        }
    }
}

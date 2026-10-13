using backend.Services.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace backend.Extensions;

/// <summary>
/// 權限需求（Policy 名稱與權限名稱相同）
/// </summary>
public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

/// <summary>
/// 判斷操作者權限一律查資料庫，不信任 JWT 內可能已過時的權限
/// </summary>
public class PermissionAuthorizationHandler(IPermissionService permissionService) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        var userId = context.User.GetUserId();
        var permissions = await permissionService.GetUserPermissionsAsync(userId);
        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}

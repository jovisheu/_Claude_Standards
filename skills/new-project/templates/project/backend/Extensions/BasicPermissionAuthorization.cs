using backend.Data;
using backend.Services.BasicPermissions;
using Microsoft.AspNetCore.Authorization;

namespace backend.Extensions;

/// <summary>登入者須為存在且啟用中的使用者（查資料庫）</summary>
public class ActiveBasicUserRequirement : IAuthorizationRequirement;

/// <summary>登入者須擁有指定權限（查資料庫，不信任 JWT 內可能已過時的權限）</summary>
public class BasicPermissionRequirement(string basicPermissionName) : IAuthorizationRequirement
{
    public string BasicPermissionName { get; } = basicPermissionName;
}

public class BasicPermissionAuthorizationHandler(IBasicPermissionsService basicPermissionsService) : IAuthorizationHandler
{
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var pending = context.PendingRequirements
            .Where(r => r is ActiveBasicUserRequirement or BasicPermissionRequirement)
            .ToList();
        if (pending.Count == 0)
        {
            return;
        }

        var basicUserId = context.User.FindBasicUserId();
        if (basicUserId is null)
        {
            return;
        }

        // 停用或不存在的使用者回 null，所有需求皆不通過
        var basicPermissionNames = await basicPermissionsService.GetBasicPermissionNamesAsync(basicUserId.Value);
        if (basicPermissionNames is null)
        {
            return;
        }

        foreach (var requirement in pending)
        {
            if (requirement is ActiveBasicUserRequirement
                || requirement is BasicPermissionRequirement permission && basicPermissionNames.Contains(permission.BasicPermissionName))
            {
                context.Succeed(requirement);
            }
        }
    }
}

public static class BasicPermissionAuthorization
{
    /// <summary>
    /// 預設 Policy 與 Fallback Policy：須登入且為啟用中的使用者（所有端點預設需授權，開放端點須明確標註 AllowAnonymous）；
    /// 每個權限名稱註冊同名 Policy
    /// </summary>
    public static IServiceCollection AddBasicPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, BasicPermissionAuthorizationHandler>();

        var builder = services.AddAuthorizationBuilder();
        var defaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new ActiveBasicUserRequirement())
            .Build();
        builder.SetDefaultPolicy(defaultPolicy);
        builder.SetFallbackPolicy(defaultPolicy);

        foreach (var name in BasicPermissionNames.All)
        {
            builder.AddPolicy(name, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new BasicPermissionRequirement(name)));
        }

        return services;
    }
}

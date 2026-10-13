using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace backend.Extensions;

/// <summary>
/// 權限不足時回 403 ApiResponse；未登入交由 JWT OnChallenge 回 401
/// </summary>
public class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            await context.Response.WriteApiResponseAsync(StatusCodes.Status403Forbidden, "權限不足，無法執行此操作");
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}

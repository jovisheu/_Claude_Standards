using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace backend.Extensions;

/// <summary>
/// 授權失敗回傳 ApiResponse：已登入但無權限回 403；未登入交由 JWT OnChallenge 回 401
/// </summary>
public class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            await ApiResponseWriter.WriteAsync(context, StatusCodes.Status403Forbidden, ApiResponseWriter.DefaultMessage(StatusCodes.Status403Forbidden));
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}

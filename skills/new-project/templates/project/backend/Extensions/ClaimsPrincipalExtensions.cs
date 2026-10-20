using System.Security.Claims;
using backend.Exceptions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace backend.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>取得登入者的使用者編號（JWT sub）；未登入時回 401</summary>
    public static int GetBasicUserId(this ClaimsPrincipal user)
        => user.FindBasicUserId() ?? throw new BusinessException("登入已逾時，請重新登入", StatusCodes.Status401Unauthorized);

    /// <summary>取得登入者的使用者編號；未登入為 null（授權處理器等不須丟出例外的地方使用）</summary>
    public static int? FindBasicUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

    /// <summary>取得登入者帳號（JWT unique_name）</summary>
    public static string? GetBasicUsername(this ClaimsPrincipal user)
        => user.Identity?.IsAuthenticated == true ? user.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value : null;
}

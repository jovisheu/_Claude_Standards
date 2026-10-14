using System.Security.Claims;
using backend.Exceptions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace backend.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>取得目前登入者 Id</summary>
    public static int GetBasicUserId(this ClaimsPrincipal basicUser)
    {
        var value = basicUser.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id)
            ? id
            : throw new BusinessException("尚未登入或登入已逾時，請重新登入", StatusCodes.Status401Unauthorized);
    }
}

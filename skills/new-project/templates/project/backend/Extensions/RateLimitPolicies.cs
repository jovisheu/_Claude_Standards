using System.Threading.RateLimiting;
using backend.Settings;

namespace backend.Extensions;

/// <summary>
/// 速率限制：以 IP 為單位的固定時間窗；超過時回 429 ApiResponse
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>登入、忘記密碼、重設密碼、逾期變更密碼</summary>
    public const string Auth = "auth";

    /// <summary>取得驗證碼</summary>
    public const string Captcha = "captcha";

    public static IServiceCollection AddApiRateLimiter(this IServiceCollection services, RateLimitSettings settings)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(Auth, context => CreateLimiter(context, Auth, settings.AuthPermitLimit, settings.WindowSeconds));
            options.AddPolicy(Captcha, context => CreateLimiter(context, Captcha, settings.CaptchaPermitLimit, settings.WindowSeconds));
            options.OnRejected = async (context, _) =>
            {
                await ApiResponseWriter.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests, ApiResponseWriter.DefaultMessage(StatusCodes.Status429TooManyRequests));
            };
        });

        return services;
    }

    private static RateLimitPartition<string> CreateLimiter(HttpContext context, string policy, int permitLimit, int windowSeconds)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter($"{policy}:{ip}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }
}

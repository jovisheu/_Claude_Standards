namespace backend.Extensions;

/// <summary>
/// 安全標頭；API 回應一律 Cache-Control: no-store（含個資或 Token，不可被快取）
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; " +
        "connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";

            // Swagger UI（僅 Development）需要行內腳本，不套用 CSP
            var isSwagger = environment.IsDevelopment() && context.Request.Path.StartsWithSegments("/swagger");
            if (!isSwagger)
            {
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
            }

            if (ApiResponseWriter.IsApiRequest(context))
            {
                headers.CacheControl = "no-store";
                headers.Pragma = "no-cache";
            }

            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            return Task.CompletedTask;
        });

        return next(context);
    }
}

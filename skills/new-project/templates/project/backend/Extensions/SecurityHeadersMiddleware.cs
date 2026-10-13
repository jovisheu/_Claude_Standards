namespace backend.Extensions;

/// <summary>
/// 安全標頭；API 回應一律不快取
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            // Swagger UI 需要內嵌指令碼，僅 Development 開放且不套用 CSP
            if (!(env.IsDevelopment() && path.StartsWithSegments("/swagger")))
            {
                headers["Content-Security-Policy"] = ContentSecurityPolicy;
            }

            // API 回應含個資或 Token，不可被瀏覽器或代理伺服器快取
            if (path.StartsWithSegments("/api"))
            {
                headers.CacheControl = "no-store";
                headers.Pragma = "no-cache";
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}

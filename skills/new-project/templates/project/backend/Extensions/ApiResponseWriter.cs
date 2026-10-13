using backend.DTOs;

namespace backend.Extensions;

/// <summary>
/// 於 Middleware 或框架事件中直接輸出 ApiResponse
/// </summary>
public static class ApiResponseWriter
{
    public static Task WriteApiResponseAsync(this HttpResponse response, int statusCode, string message, CancellationToken cancellationToken = default)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        return response.WriteAsJsonAsync(ApiResponse.Fail(statusCode, message), cancellationToken);
    }
}

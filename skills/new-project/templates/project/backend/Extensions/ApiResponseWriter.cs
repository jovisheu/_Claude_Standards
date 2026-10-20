using System.Text.Json;
using backend.DTOs;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace backend.Extensions;

/// <summary>
/// 於 Middleware、驗證與速率限制等非 Controller 處直接寫出 ApiResponse
/// </summary>
public static class ApiResponseWriter
{
    private static readonly JsonSerializerOptions FallbackOptions = new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";

        var options = context.RequestServices.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions ?? FallbackOptions;
        await JsonSerializer.SerializeAsync(context.Response.Body, ApiResponse.Fail(statusCode, message), options);
    }

    /// <summary>各狀態碼的預設訊息</summary>
    public static string DefaultMessage(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "請求資料格式不正確",
        StatusCodes.Status401Unauthorized => "登入已逾時，請重新登入",
        StatusCodes.Status403Forbidden => "您沒有執行此操作的權限",
        StatusCodes.Status404NotFound => "找不到指定的資源",
        StatusCodes.Status405MethodNotAllowed => "不支援此請求方法",
        StatusCodes.Status413PayloadTooLarge => "請求資料過大",
        StatusCodes.Status415UnsupportedMediaType => "不支援的內容類型，請以 JSON 格式送出",
        StatusCodes.Status429TooManyRequests => "請求過於頻繁，請稍後再試",
        _ when statusCode >= 500 => "系統發生異常，請聯繫管理員",
        _ => "請求無法處理",
    };

    /// <summary>判斷是否為 API 路徑（PathBase 已由 IIS 移除）</summary>
    public static bool IsApiRequest(HttpContext context)
        => context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
}

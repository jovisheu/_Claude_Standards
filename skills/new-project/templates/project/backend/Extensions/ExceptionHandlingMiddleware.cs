using backend.Exceptions;

namespace backend.Extensions;

/// <summary>
/// 全域例外處理：業務錯誤回傳其訊息，其他例外寫入 Log、對外只回通用訊息
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BusinessException ex)
        {
            if (context.Response.HasStarted) throw;
            await context.Response.WriteApiResponseAsync(ex.StatusCode, ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            if (context.Response.HasStarted) throw;
            logger.LogWarning("請求格式錯誤：{Path} {StatusCode}", context.Request.Path, ex.StatusCode);
            await context.Response.WriteApiResponseAsync(ex.StatusCode, "請求資料格式不正確");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // 用戶端中斷連線，不需回應
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "未處理的例外：{Method} {Path}", context.Request.Method, context.Request.Path);
            if (context.Response.HasStarted) throw;
            await context.Response.WriteApiResponseAsync(StatusCodes.Status500InternalServerError, "系統發生異常，請聯繫管理員");
        }
    }
}

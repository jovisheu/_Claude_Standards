using backend.Exceptions;
using backend.Services.BasicExceptionlogs;

namespace backend.Extensions;

/// <summary>
/// 全域例外處理：BusinessException 回傳其訊息；其他例外寫入 ILogger 與 BasicExceptionlog，對外只回通用訊息（不含 StackTrace）。
/// 回應已開始輸出時無法改寫，重新丟出讓伺服器中止連線，不吞掉例外
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IBasicExceptionlogsService basicExceptionlogsService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BusinessException ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await ApiResponseWriter.WriteAsync(context, ex.StatusCode, ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            // 請求本身不正確（如 Body 過大、連線中斷），不視為系統錯誤
            logger.LogWarning("不正確的請求：{StatusCode} {Path}", ex.StatusCode, context.Request.Path.Value);
            if (context.Response.HasStarted)
            {
                throw;
            }

            await ApiResponseWriter.WriteAsync(context, ex.StatusCode, ApiResponseWriter.DefaultMessage(ex.StatusCode));
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // 用戶端已中斷連線，不需回應
        }
        catch (Exception ex)
        {
            var methodName = context.GetMethodName();
            logger.LogError(ex, "未處理的例外：{MethodName}", methodName);
            await basicExceptionlogsService.WriteAsync(ex, methodName, context.GetUsername());
            if (context.Response.HasStarted)
            {
                throw;
            }

            await ApiResponseWriter.WriteAsync(context, StatusCodes.Status500InternalServerError, ApiResponseWriter.DefaultMessage(StatusCodes.Status500InternalServerError));
        }
    }
}

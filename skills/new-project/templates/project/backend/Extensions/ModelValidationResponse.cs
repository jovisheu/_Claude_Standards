using backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace backend.Extensions;

/// <summary>
/// 模型驗證失敗時回傳 ApiResponse；JSON 解析失敗或缺少 Body 只回通用訊息，不洩漏型別或路徑
/// </summary>
public static class ModelValidationResponse
{
    private const string InvalidFormatMessage = "請求資料格式不正確";

    public static IActionResult Create(ActionContext context)
    {
        var bodyParameterNames = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invalidEntries = context.ModelState
            .Where(kv => kv.Value?.ValidationState == ModelValidationState.Invalid)
            .ToList();

        // JSON 解析錯誤的 key 以 $ 開頭；缺少 Body 時 key 為參數名稱；型別轉換錯誤帶有 Exception
        var isFormatError = invalidEntries.Any(kv =>
            kv.Key.StartsWith('$') ||
            bodyParameterNames.Contains(kv.Key) ||
            kv.Value!.Errors.Any(e => e.Exception != null));

        string message;
        if (isFormatError)
        {
            message = InvalidFormatMessage;
        }
        else
        {
            var messages = invalidEntries
                .SelectMany(kv => kv.Value!.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? InvalidFormatMessage : e.ErrorMessage)
                .Distinct()
                .ToList();
            message = messages.Count == 0 ? InvalidFormatMessage : string.Join("\n", messages);
        }

        return new BadRequestObjectResult(ApiResponse.Fail(StatusCodes.Status400BadRequest, message));
    }
}

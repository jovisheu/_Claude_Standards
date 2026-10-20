using backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace backend.Extensions;

/// <summary>
/// 模型驗證失敗時回傳 ApiResponse（400）；JSON 解析失敗或缺少 Body 時只回「請求資料格式不正確」，不洩漏型別或路徑
/// </summary>
public static class ModelValidationResponse
{
    public const string FormatErrorMessage = "請求資料格式不正確";

    public static IActionResult Create(ActionContext context)
    {
        var message = IsFormatError(context)
            ? FormatErrorMessage
            : string.Join("\n", context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? FormatErrorMessage : e.ErrorMessage)
                .Distinct());
        if (string.IsNullOrWhiteSpace(message))
        {
            message = FormatErrorMessage;
        }

        return new BadRequestObjectResult(ApiResponse.Fail(StatusCodes.Status400BadRequest, message));
    }

    private static bool IsFormatError(ActionContext context)
    {
        var bodyParameterNames = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.ValidationState != ModelValidationState.Invalid)
            {
                continue;
            }

            // System.Text.Json 的錯誤以 $ 開頭；缺少 Body 時鍵為空字串或參數名稱；例外造成的錯誤也視為格式錯誤
            if (key.Length == 0 || key.StartsWith('$') || bodyParameterNames.Contains(key) || entry.Errors.Any(e => e.Exception is not null))
            {
                return true;
            }
        }

        return false;
    }
}

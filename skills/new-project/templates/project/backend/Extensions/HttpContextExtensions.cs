using Microsoft.AspNetCore.Mvc.Controllers;

namespace backend.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// 取目前請求的 Controller 與 Action（如 BasicUsersController.Create），無法辨識時為 Unknown；
    /// async 方法例外的 TargetSite 多為 MoveNext，因此改由端點取得
    /// </summary>
    public static string GetMethodName(this HttpContext? context)
    {
        var action = context?.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        return action != null ? $"{action.ControllerTypeInfo.Name}.{action.MethodInfo.Name}" : "Unknown";
    }
}

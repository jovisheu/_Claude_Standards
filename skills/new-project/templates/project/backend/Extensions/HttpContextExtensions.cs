using Microsoft.AspNetCore.Mvc.Controllers;

namespace backend.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// 取得目前端點的類別與方法名稱（如 BasicUsersController.Create），供系統錯誤紀錄與異動歷程使用；無法辨識時為 Unknown。
    /// 以實際的類別與方法名稱為準（ActionName 會去掉 Async 字尾、受 [ActionName] 影響）
    /// </summary>
    public static string GetMethodName(this HttpContext? context)
    {
        var action = context?.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        return action is not null ? $"{action.ControllerTypeInfo.Name}.{action.MethodInfo.Name}" : "Unknown";
    }

    /// <summary>取得登入者帳號；未登入為 null</summary>
    public static string? GetUsername(this HttpContext? context) => context?.User.GetBasicUsername();
}

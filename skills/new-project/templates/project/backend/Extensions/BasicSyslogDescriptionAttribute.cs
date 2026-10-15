namespace backend.Extensions;

/// <summary>
/// 寫入端點的異動歷程預設說明（選填，如「編輯公告」）；Service 以 BasicSyslogContext.Describe 指定時以其為準。
/// 只放固定文字，不可含機密或個資
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class BasicSyslogDescriptionAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}

namespace backend.Extensions;

/// <summary>
/// 寫入端點的異動歷程預設業務說明（Service 以 BasicSyslogContext.Describe 指定時優先）
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class BasicSyslogDescriptionAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}

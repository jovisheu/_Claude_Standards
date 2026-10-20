using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace backend.Extensions;

/// <summary>
/// 輸入含 HTML 標籤時直接拒絕（不編碼後存入，避免二次編碼）
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public partial class NoHtmlAttribute : ValidationAttribute
{
    [GeneratedRegex(@"<\s*[a-zA-Z!/?]", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();

    public NoHtmlAttribute() : base("{0}不可包含 HTML 標籤")
    {
    }

    public override bool IsValid(object? value)
        => value is not string text || !HtmlTagRegex().IsMatch(text);

    public override string FormatErrorMessage(string name) => string.Format(ErrorMessageString, name);
}

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace backend.Extensions;

/// <summary>
/// 拒絕含 HTML 標籤的輸入（直接拒絕，不編碼後存入）
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public partial class NoHtmlAttribute : ValidationAttribute
{
    public NoHtmlAttribute() : base("{0}不可包含 HTML 標籤") { }

    public override bool IsValid(object? value)
        => value is not string text || !HtmlTagRegex().IsMatch(text);

    [GeneratedRegex(@"<\s*[a-zA-Z/!?]|&#|&[a-zA-Z]+;", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex HtmlTagRegex();
}

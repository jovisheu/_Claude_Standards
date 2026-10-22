using System.Net;

namespace backend.Services.Mail;

/// <summary>
/// 郵件範本（Singleton）：放在後端非公開目錄 Template/，於啟動時載入，檔案遺失即無法啟動；
/// 放入的動態資料一律先 HTML 編碼
/// </summary>
public class MailTemplateStore
{
    public const string ResetPassword = "ResetPassword";
    public const string PasswordExpireNotice = "PasswordExpireNotice";

    private readonly Dictionary<string, string> _templates = new(StringComparer.Ordinal);

    public MailTemplateStore(IWebHostEnvironment environment)
    {
        foreach (var name in new[] { ResetPassword, PasswordExpireNotice })
        {
            var path = Path.Combine(environment.ContentRootPath, "Template", $"{name}.html");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"找不到郵件範本 Template/{name}.html");
            }

            _templates[name] = File.ReadAllText(path);
        }
    }

    public string Render(string name, IReadOnlyDictionary<string, string> values)
    {
        var html = _templates[name];
        foreach (var (key, value) in values)
        {
            // 以 WebUtility 編碼（中文維持原字，不轉成 &#x...;）
            html = html.Replace($"{{{{{key}}}}}", WebUtility.HtmlEncode(value), StringComparison.Ordinal);
        }

        return html;
    }
}

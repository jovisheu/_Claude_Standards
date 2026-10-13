namespace backend.Services.Mail;

/// <summary>
/// 郵件範本於啟動時載入，檔案遺失即無法啟動（避免到寄信時才出錯）
/// </summary>
public class MailTemplateStore
{
    public const string ResetPassword = "ResetPassword";

    private readonly Dictionary<string, string> _templates = [];

    public MailTemplateStore(IWebHostEnvironment env)
    {
        // 範本名稱為固定清單，不以使用者輸入組成路徑
        foreach (var name in new[] { ResetPassword })
        {
            var path = Path.Combine(env.ContentRootPath, "Template", name + ".html");
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"找不到郵件範本 Template/{name}.html");
            }
            _templates[name] = File.ReadAllText(path);
        }
    }

    public string Get(string name) => _templates[name];
}

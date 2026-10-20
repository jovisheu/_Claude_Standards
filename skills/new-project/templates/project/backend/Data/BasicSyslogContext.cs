namespace backend.Data;

/// <summary>
/// 異動歷程的業務說明（Scoped）：Service 在 SaveChangesAsync 前呼叫 Describe，優先於端點的 [BasicSyslogDescription]。
/// 說明會顯示於歷程畫面並轉存 txt，不可含密碼、token 等機密或不必要的個資。
/// </summary>
public class BasicSyslogContext
{
    public const int DescriptionMaxLength = 200;

    private string? _description;

    /// <summary>指定下一次存檔的業務說明（去除前後空白、超過長度截斷）</summary>
    public void Describe(string? description)
    {
        var text = description?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            _description = null;
            return;
        }

        _description = text.Length > DescriptionMaxLength ? text[..DescriptionMaxLength] : text;
    }

    /// <summary>取出並清除說明（只在有需記錄的異動時由攔截器呼叫）</summary>
    public string? Take()
    {
        var description = _description;
        _description = null;
        return description;
    }
}

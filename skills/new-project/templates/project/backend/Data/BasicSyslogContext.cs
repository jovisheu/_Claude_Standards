namespace backend.Data;

/// <summary>
/// 指定下一次存檔的異動歷程說明（選填）。在 SaveChangesAsync 前呼叫 Describe，
/// 寫入歷程時使用後即清除；未指定時取端點的 BasicSyslogDescription。以 Scoped 註冊
/// </summary>
public sealed class BasicSyslogContext
{
    public const int DescriptionMaxLength = 200;

    private string? _description;

    /// <summary>
    /// 設定說明（如「審核通過」「Excel 匯入 120 筆」）；不可含密碼、token 等機密或不必要的個資，
    /// 說明會顯示於歷程畫面並轉存至 txt。超過長度時截斷
    /// </summary>
    public void Describe(string? description)
    {
        var text = description?.Trim();
        _description = string.IsNullOrEmpty(text) ? null
            : text.Length > DescriptionMaxLength ? text[..DescriptionMaxLength] : text;
    }

    /// <summary>取出說明並清除，避免帶到下一次存檔</summary>
    internal string? Take()
    {
        var description = _description;
        _description = null;
        return description;
    }
}

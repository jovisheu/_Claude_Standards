namespace backend.Settings;

/// <summary>
/// SMTP 設定（appsettings.json 的 Smtp 區段）；Password 於設定檔為 AES 加密值（空字串表示不需驗證），啟動時解密
/// </summary>
public class SmtpSettings
{
    public const string SectionName = "Smtp";

    /// <summary>false 時不寄出，改寫入 PickupDirectory（開發測試用）</summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string SenderName { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>未啟用時郵件寫入的目錄（相對於網站目錄）</summary>
    public string PickupDirectory { get; set; } = "MailPickup";
}

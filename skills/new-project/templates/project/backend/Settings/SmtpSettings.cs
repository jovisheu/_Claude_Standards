namespace backend.Settings;

/// <summary>
/// SMTP 設定（Password 於啟動時解密後以 PostConfigure 設定）
/// </summary>
public class SmtpSettings
{
    public const string SectionName = "Smtp";

    /// <summary>false 時不實際寄送，改寫入 PickupDirectory</summary>
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PickupDirectory { get; set; } = "MailPickup";
}

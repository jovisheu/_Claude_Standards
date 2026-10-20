namespace backend.Settings;

/// <summary>
/// 系統異動歷程設定（appsettings.json 的 BasicSyslog 區段）
/// </summary>
public class BasicSyslogSettings
{
    public const string SectionName = "BasicSyslog";

    /// <summary>資料庫保留天數，超過者轉存至 Logs\Syslog 後刪除</summary>
    public int RetentionDays { get; set; } = 30;
}

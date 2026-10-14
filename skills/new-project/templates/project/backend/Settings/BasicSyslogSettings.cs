namespace backend.Settings;

/// <summary>
/// 系統異動歷程設定
/// </summary>
public class BasicSyslogSettings
{
    public const string SectionName = "BasicSyslog";

    /// <summary>資料庫保留天數，超過即轉存至 Logs\Syslog\YYYY\MM\DD\{MethodName}.txt 後刪除</summary>
    public int RetentionDays { get; set; } = 30;
}

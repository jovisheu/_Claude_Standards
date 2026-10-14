namespace backend.Settings;

/// <summary>
/// 系統錯誤紀錄設定
/// </summary>
public class BasicExceptionlogSettings
{
    public const string SectionName = "BasicExceptionlog";

    /// <summary>資料庫保留天數，超過即轉存至 Logs\Exception\YYYY\MM\DD\{MethodName}.txt 後刪除</summary>
    public int RetentionDays { get; set; } = 30;
}

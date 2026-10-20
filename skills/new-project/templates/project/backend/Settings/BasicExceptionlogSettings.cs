namespace backend.Settings;

/// <summary>
/// 系統錯誤紀錄設定（appsettings.json 的 BasicExceptionlog 區段）
/// </summary>
public class BasicExceptionlogSettings
{
    public const string SectionName = "BasicExceptionlog";

    /// <summary>資料庫保留天數，超過者轉存至 Logs\Exception 後刪除</summary>
    public int RetentionDays { get; set; } = 30;
}

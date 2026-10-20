namespace backend.Settings;

/// <summary>
/// 系統設定（appsettings.json 的 App 區段）
/// </summary>
public class AppSettings
{
    public const string SectionName = "App";

    /// <summary>系統名稱（郵件、頁面標題）</summary>
    public string SystemName { get; set; } = string.Empty;

    /// <summary>前端網址（含部署子路徑，不含結尾斜線）；重設密碼連結一律以此組成，不使用請求的 Host 標頭</summary>
    public string FrontendUrl { get; set; } = string.Empty;
}

namespace backend.Settings;

/// <summary>
/// 系統設定
/// </summary>
public class AppSettings
{
    public const string SectionName = "App";

    public string SystemName { get; set; } = string.Empty;

    /// <summary>前端網址（含部署子路徑），用於組成重設密碼連結，不可取自請求的 Host 標頭</summary>
    public string FrontendUrl { get; set; } = string.Empty;
}

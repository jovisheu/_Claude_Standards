namespace backend.Extensions;

/// <summary>
/// 紀錄轉存檔路徑：網站目錄\Logs\{類別}\YYYY\MM\DD\{MethodName}.txt（Logs 不在 wwwroot 下，不會被下載）
/// </summary>
public static class LogArchive
{
    public static string GetFilePath(string contentRootPath, string category, DateTime date, string methodName) =>
        Path.Combine(contentRootPath, "Logs", category,
            date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd"), ToFileName(methodName) + ".txt");

    /// <summary>MethodName 作為檔名前移除不合法字元，避免路徑跳脫</summary>
    private static string ToFileName(string methodName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var name = new string(methodName.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim('.', ' ');
        return string.IsNullOrEmpty(name) ? "Unknown" : name;
    }
}

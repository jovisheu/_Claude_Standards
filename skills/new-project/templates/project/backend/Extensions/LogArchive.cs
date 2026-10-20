using System.Text;

namespace backend.Extensions;

/// <summary>
/// 系統紀錄轉存：網站目錄\Logs\{類別}\YYYY\MM\DD\{MethodName}.txt（不在 wwwroot 下，不會被下載）
/// </summary>
public static class LogArchive
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    public static string GetFilePath(string contentRootPath, string category, DateTime date, string methodName)
    {
        var directory = Path.Combine(contentRootPath, "Logs", category, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd"));
        return Path.Combine(directory, $"{ToSafeFileName(methodName)}.txt");
    }

    /// <summary>附加寫入（同檔附加）；目錄不存在時建立</summary>
    public static async Task AppendAsync(string filePath, string content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.AppendAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);
    }

    /// <summary>MethodName 作為檔名前移除不合法字元與路徑分隔</summary>
    public static string ToSafeFileName(string methodName)
    {
        var builder = new StringBuilder(methodName.Length);
        foreach (var c in methodName)
        {
            if (!InvalidFileNameChars.Contains(c) && !char.IsControl(c))
            {
                builder.Append(c);
            }
        }

        // 移除前後的點與空白，避免 ".."、隱藏檔或 Windows 不接受的結尾
        var name = builder.ToString().Trim().Trim('.').Trim();
        if (name.Length > 100)
        {
            name = name[..100];
        }

        return name.Length == 0 ? "Unknown" : name;
    }
}

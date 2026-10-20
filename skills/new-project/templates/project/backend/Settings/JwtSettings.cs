namespace backend.Settings;

/// <summary>
/// JWT 設定（appsettings.json 的 Jwt 區段）；Key 於設定檔為 AES 加密值，啟動時解密後以 PostConfigure 寫入
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>簽章金鑰（解密後為 Base64 字串）</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Token 有效分鐘數</summary>
    public int ExpireMinutes { get; set; } = 60;
}

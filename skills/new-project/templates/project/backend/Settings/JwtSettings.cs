namespace backend.Settings;

/// <summary>
/// JWT 設定（Key 於啟動時解密後以 PostConfigure 設定）
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; } = 60;
}

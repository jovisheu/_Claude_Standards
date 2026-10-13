using System.Security.Cryptography;
using System.Text;

namespace backend.Extensions;

/// <summary>
/// appsettings.json 機密設定的 AES-256 加解密（CBC、PKCS7，加密值以 Base64 儲存）
/// </summary>
public static class ConfigEncryption
{
    private const string KeyName = "AES256Key";
    private const string IvName = "AES256IV";

    /// <summary>加密明文，回傳 Base64</summary>
    public static string Encrypt(IConfiguration configuration, string plainText)
    {
        using var aes = CreateAes(configuration);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = aes.EncryptCbc(plainBytes, aes.IV, PaddingMode.PKCS7);
        return Convert.ToBase64String(cipherBytes);
    }

    /// <summary>
    /// 解密必要的設定值；空白、解密失敗或金鑰長度不符時拒絕啟動（錯誤訊息不含原始值）
    /// </summary>
    public static string DecryptRequired(IConfiguration configuration, string settingName)
    {
        var cipherText = configuration[settingName];
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            throw new InvalidOperationException($"設定值 {settingName} 未設定，請以 dotnet run -- encrypt \"<明文>\" 產生加密值後填入");
        }

        return Decrypt(configuration, settingName, cipherText);
    }

    /// <summary>
    /// 解密選填的設定值；空白時回傳空字串（不解密）
    /// </summary>
    public static string DecryptOptional(IConfiguration configuration, string settingName)
    {
        var cipherText = configuration[settingName];
        return string.IsNullOrWhiteSpace(cipherText) ? string.Empty : Decrypt(configuration, settingName, cipherText);
    }

    private static string Decrypt(IConfiguration configuration, string settingName, string cipherText)
    {
        using var aes = CreateAes(configuration);
        try
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            var plainBytes = aes.DecryptCbc(cipherBytes, aes.IV, PaddingMode.PKCS7);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            // 不附上原始例外訊息與設定值，避免洩漏
            throw new InvalidOperationException($"設定值 {settingName} 解密失敗，請確認是否以本專案的 {KeyName}/{IvName} 產生加密值");
        }
    }

    private static Aes CreateAes(IConfiguration configuration)
    {
        var key = Encoding.UTF8.GetBytes(configuration[KeyName] ?? string.Empty);
        var iv = Encoding.UTF8.GetBytes(configuration[IvName] ?? string.Empty);
        if (key.Length != 32 || iv.Length != 16)
        {
            throw new InvalidOperationException($"{KeyName} 須為 32 字元、{IvName} 須為 16 字元");
        }

        var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = key;
        aes.IV = iv;
        return aes;
    }
}

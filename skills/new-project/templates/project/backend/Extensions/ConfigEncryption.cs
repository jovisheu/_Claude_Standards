using System.Security.Cryptography;
using System.Text;

namespace backend.Extensions;

/// <summary>
/// appsettings.json 機密設定的 AES-256 加解密（.NET 內建 Aes，CBC、PKCS7，密文以 Base64 儲存）；
/// 金鑰 AES256Key（32 字元）、IV AES256IV（16 字元）每個專案各自隨機產生
/// </summary>
public static class ConfigEncryption
{
    public const string KeyName = "AES256Key";
    public const string IvName = "AES256IV";

    public static string Encrypt(string plainText, string key, string iv)
    {
        using var aes = CreateAes(key, iv);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = aes.EncryptCbc(plainBytes, aes.IV, PaddingMode.PKCS7);
        return Convert.ToBase64String(cipherBytes);
    }

    /// <summary>解密；失敗時丟出不含原始值的例外（拒絕啟動）</summary>
    public static string Decrypt(string cipherText, string key, string iv, string settingName)
    {
        try
        {
            using var aes = CreateAes(key, iv);
            var cipherBytes = Convert.FromBase64String(cipherText);
            var plainBytes = aes.DecryptCbc(cipherBytes, aes.IV, PaddingMode.PKCS7);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            throw new InvalidOperationException($"設定值 {settingName} 解密失敗，請確認為本專案 {KeyName}／{IvName} 產生的加密值");
        }
    }

    /// <summary>讀取並驗證設定檔中的金鑰與 IV</summary>
    public static (string Key, string Iv) ReadKey(IConfiguration configuration)
    {
        var key = configuration[KeyName] ?? string.Empty;
        var iv = configuration[IvName] ?? string.Empty;
        if (Encoding.UTF8.GetByteCount(key) != 32 || Encoding.UTF8.GetByteCount(iv) != 16)
        {
            throw new InvalidOperationException($"設定檔的 {KeyName} 須為 32 字元、{IvName} 須為 16 字元");
        }

        return (key, iv);
    }

    private static Aes CreateAes(string key, string iv)
    {
        var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);
        return aes;
    }
}

using System.Security.Cryptography;

// new-project 一次性工具：產生 AES 金鑰／IV、JWT 金鑰與預設管理員、一般使用者（訪客）密碼雜湊
// 用法：dotnet run --project <scratchpad>\keygen -- "<管理員初始密碼>" "<訪客初始密碼>"
// 輸出含機密，只供建立專案時填入範本，不得寫入回報或專案以外的檔案

const string chars ="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

Console.WriteLine("AES256Key=" + RandomNumberGenerator.GetString(chars, 32));
Console.WriteLine("AES256IV=" + RandomNumberGenerator.GetString(chars, 16));
// TokenService 以 Base64 解碼 JWT 金鑰，須為 Base64 字串
Console.WriteLine("JwtKey=" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));

if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
{
    Console.WriteLine("AdminPasswordHash=" + BCrypt.Net.BCrypt.HashPassword(args[0], 12));
}

if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
{
    Console.WriteLine("GuestPasswordHash=" + BCrypt.Net.BCrypt.HashPassword(args[1], 12));
}

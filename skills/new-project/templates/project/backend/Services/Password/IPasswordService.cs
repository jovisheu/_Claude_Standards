using backend.Models;

namespace backend.Services.Password;

public interface IPasswordService
{
    /// <summary>
    /// 設定密碼的唯一方法（新增、編輯、逾期變更、重設皆呼叫此方法）：檢查規則與歷程、寫入雜湊、記錄歷程並作廢重設連結；
    /// 不呼叫 SaveChangesAsync，由呼叫端與其他異動一起存檔。不符規則時丟出 BusinessException。
    /// </summary>
    Task SetPasswordAsync(BasicUser basicUser, string newPassword);

    /// <summary>驗證密碼雜湊</summary>
    bool Verify(string password, string passwordHash);

    /// <summary>密碼是否已逾期（無歷程視為逾期）</summary>
    Task<bool> IsExpiredAsync(string username);
}

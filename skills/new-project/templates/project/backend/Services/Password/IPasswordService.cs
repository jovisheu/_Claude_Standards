using backend.Models;

namespace backend.Services.Password;

public interface IPasswordService
{
    /// <summary>
    /// 設定密碼的唯一方法：檢查規則與歷程、寫入雜湊、記錄歷程並作廢重設連結。
    /// 只修改追蹤中的實體，由呼叫端統一 SaveChangesAsync；不符規則時丟 BusinessException。
    /// </summary>
    Task SetPasswordAsync(User user, string newPassword);

    /// <summary>驗證密碼</summary>
    bool Verify(string password, string passwordHash);

    /// <summary>密碼是否已逾期（無歷程視為逾期）</summary>
    Task<bool> IsExpiredAsync(string username);
}

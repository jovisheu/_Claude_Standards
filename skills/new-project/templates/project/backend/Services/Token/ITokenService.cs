using backend.Models;

namespace backend.Services.Token;

public interface ITokenService
{
    /// <summary>產生登入用 JWT 與到期時間</summary>
    (string Token, DateTime ExpiresAt) CreateAccessToken(BasicUser basicUser);

    /// <summary>產生逾期變更密碼的一次性、限時憑證（不是 JWT，不可用於呼叫其他 API）</summary>
    string CreatePasswordChangeToken(int basicUserId);

    /// <summary>取得變更憑證對應的使用者編號；無效或過期回 null（不消耗）</summary>
    int? GetPasswordChangeBasicUserId(string token);

    /// <summary>作廢變更憑證（變更成功後呼叫）</summary>
    void RevokePasswordChangeToken(string token);
}

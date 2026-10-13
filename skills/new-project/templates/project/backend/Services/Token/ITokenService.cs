using backend.Models;

namespace backend.Services.Token;

public interface ITokenService
{
    /// <summary>產生 JWT 與到期時間</summary>
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);
}

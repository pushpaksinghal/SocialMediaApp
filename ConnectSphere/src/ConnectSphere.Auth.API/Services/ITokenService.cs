using ConnectSphere.Auth.API.Models;

namespace ConnectSphere.Auth.API.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task StoreRefreshTokenAsync(int userId, string refreshToken);
    Task<int?> ValidateRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task BlacklistAccessTokenAsync(string accessToken, TimeSpan remaining);
    Task<bool> IsAccessTokenBlacklistedAsync(string accessToken);
}
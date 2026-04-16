using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ConnectSphere.Auth.API.Models;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace ConnectSphere.Auth.API.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly IConnectionMultiplexer _redis;

    public TokenService(IConfiguration config, IConnectionMultiplexer redis)
    {
        _config = config;
        _redis = redis;
    }

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, "User")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(_config["Jwt:AccessTokenMinutes"]!)),
            signingCredentials: new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public async Task StoreRefreshTokenAsync(int userId, string refreshToken)
    {
        var db = _redis.GetDatabase();
        var expiry = TimeSpan.FromDays(
            double.Parse(_config["Jwt:RefreshTokenDays"]!));

        // Store: refreshToken → userId  (for lookup on refresh)
        await db.StringSetAsync(
            $"refresh:{refreshToken}", userId.ToString(), expiry);
    }

    public async Task<int?> ValidateRefreshTokenAsync(string refreshToken)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"refresh:{refreshToken}");

        if (value.IsNullOrEmpty) return null;
        return int.Parse(value!);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"refresh:{refreshToken}");
    }

    public async Task BlacklistAccessTokenAsync(string accessToken, TimeSpan remaining)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"blacklist:{accessToken}", "1", remaining);
    }

    public async Task<bool> IsAccessTokenBlacklistedAsync(string accessToken)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync($"blacklist:{accessToken}");
    }
}
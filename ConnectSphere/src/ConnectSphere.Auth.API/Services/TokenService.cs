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
        try
        {
             var db = _redis.GetDatabase();
        var expiry = TimeSpan.FromDays(
            double.Parse(_config["Jwt:RefreshTokenDays"]!));

        // Store: refreshToken → userId  (for lookup on refresh)
        await db.StringSetAsync(
            $"refresh:{refreshToken}", userId.ToString(), expiry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error storing refresh token: {ex.Message}");
        }
    }

    public async Task<int?> ValidateRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"refresh:{refreshToken}");

        if (value.IsNullOrEmpty) return null;
        return int. Parse(value!);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating refresh token: {ex.Message}");
            return null;
        }
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
{
    try
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"refresh:{refreshToken}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Redis unavailable, skipping token revocation: {ex.Message}");
    }
}

    public async Task BlacklistAccessTokenAsync(string accessToken, TimeSpan remaining)
{
    try
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"blacklist:{accessToken}", "1", remaining);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Redis unavailable, skipping token blacklist: {ex.Message}");
    }
}

    public async Task<bool> IsAccessTokenBlacklistedAsync(string accessToken)
{
    try
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync($"blacklist:{accessToken}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Redis unavailable, token assumed not blacklisted: {ex.Message}");
        return false;
    }
}
}
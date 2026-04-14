using Azure.Storage.Blobs;
using ConnectSphere.Auth.API.Data;
using ConnectSphere.Auth.API.DTOs;
using ConnectSphere.Auth.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Auth.API.Services;

public class AuthService : IAuthService
{
    private readonly AuthDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher<User> _hasher;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AuthDbContext db,
        ITokenService tokenService,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _hasher = new PasswordHasher<User>();
        _config = config;
        _logger = logger;
    }

    // ── Register ────────────────────────────────────────────────────────────
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check uniqueness
        bool exists = await _db.Users.AnyAsync(u =>
            u.Email == request.Email || u.UserName == request.UserName);

        if (exists)
            throw new InvalidOperationException(
                "Email or username already taken.");

        var user = new User
        {
            UserName = request.UserName,
            FullName = request.FullName,
            Email    = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await BuildAuthResponseAsync(user);
    }

    // ── Login ────────────────────────────────────────────────────────────────
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var result = _hasher.VerifyHashedPassword(
            user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid credentials.");

        return await BuildAuthResponseAsync(user);
    }

    // ── Logout ───────────────────────────────────────────────────────────────
    public async Task LogoutAsync(int userId, string accessToken, string refreshToken)
    {
        // Parse remaining lifetime from the JWT so we only blacklist until expiry
        var handler  = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt      = handler.ReadJwtToken(accessToken);
        var remaining = jwt.ValidTo - DateTime.UtcNow;

        if (remaining > TimeSpan.Zero)
            await _tokenService.BlacklistAccessTokenAsync(accessToken, remaining);

        await _tokenService.RevokeRefreshTokenAsync(refreshToken);
    }

    // ── Refresh Token ────────────────────────────────────────────────────────
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var userId = await _tokenService.ValidateRefreshTokenAsync(refreshToken);
        if (userId is null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var user = await _db.Users.FindAsync(userId);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive.");

        // Rotate: revoke old, issue new
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);
        return await BuildAuthResponseAsync(user);
    }

    // ── Update Profile ───────────────────────────────────────────────────────
    public async Task<UserProfileDto> UpdateProfileAsync(
        int userId, UpdateProfileRequest request, IFormFile? avatar)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (request.FullName is not null)
            user.FullName = request.FullName;

        if (request.Bio is not null)
            user.Bio = request.Bio;

        if (avatar is not null)
            user.AvatarUrl = await UploadAvatarAsync(userId, avatar);

        await _db.SaveChangesAsync();
        return MapToDto(user);
    }

    // ── Change Password ──────────────────────────────────────────────────────
    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var result = _hasher.VerifyHashedPassword(
            user, user.PasswordHash, request.CurrentPassword);

        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Current password is incorrect.");

        await _db.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                u => u.PasswordHash,
                _hasher.HashPassword(user, request.NewPassword)));
    }

    // ── Toggle Privacy ───────────────────────────────────────────────────────
    public async Task TogglePrivacyAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        await _db.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                u => u.IsPrivate, !user.IsPrivate));
    }

    // ── Get Profile ──────────────────────────────────────────────────────────
    public async Task<UserProfileDto?> GetProfileAsync(string userName)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == userName);

        return user is null ? null : MapToDto(user);
    }

    // ── Search Users ─────────────────────────────────────────────────────────
    public async Task<List<UserProfileDto>> SearchUsersAsync(string query)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive &&
                (EF.Functions.ILike(u.UserName, $"%{query}%") ||
                 EF.Functions.ILike(u.FullName,  $"%{query}%")))
            .Take(20)
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }

    // ── Get Suggestions ──────────────────────────────────────────────────────
    public async Task<List<UserProfileDto>> GetSuggestionsAsync(int userId)
    {
        // Returns active users who are not the current user
        // (Follow-Service will filter out already-followed users via HTTP call
        //  in a later integration step)
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.UserId != userId)
            .OrderByDescending(u => u.FollowerCount)
            .Take(10)
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }

    // ── Deactivate Account ───────────────────────────────────────────────────
    public async Task DeactivateAccountAsync(int userId)
    {
        await _db.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false));
    }

    // ── External Login (Google / GitHub) ─────────────────────────────────────
    public async Task<ExternalAuthResponse> ExternalLoginAsync(
        string email,
        string fullName,
        string userName,
        string provider)
    {
        // Check if user already exists with this email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        bool isNewUser = false;

        if (user is null)
        {
            // New user — create account automatically
            // Make username unique if taken
            var baseUserName = userName.Replace(" ", "").ToLower();
            var finalUserName = baseUserName;
            var count = 1;

            while (await _db.Users.AnyAsync(u => u.UserName == finalUserName))
            {
                finalUserName = $"{baseUserName}{count}";
                count++;
            }

            user = new User
            {
                Email        = email,
                FullName     = fullName,
                UserName     = finalUserName,
                PasswordHash = string.Empty, // No password for OAuth users
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            isNewUser = true;

            _logger.LogInformation(
                "New user {Email} registered via {Provider}",
                email, provider);
        }
        else if (!user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Account is suspended.");
        }

        var authResponse = await BuildAuthResponseAsync(user);

        return new ExternalAuthResponse
        {
            AccessToken  = authResponse.AccessToken,
            RefreshToken = authResponse.RefreshToken,
            User         = authResponse.User,
            IsNewUser    = isNewUser
        };
    }
    // ── Helpers ──────────────────────────────────────────────────────────────
    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var accessToken  = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        await _tokenService.StoreRefreshTokenAsync(user.UserId, refreshToken);

        return new AuthResponse
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            User         = MapToDto(user)
        };
    }

    private async Task<string> UploadAvatarAsync(int userId, IFormFile file)
    {
        var connStr    = _config["AzureBlob:ConnectionString"]!;
        var container  = _config["AzureBlob:ContainerName"]!;
        var blobClient = new BlobContainerClient(connStr, container);

        await blobClient.CreateIfNotExistsAsync();

        var blobName = $"avatars/{userId}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var blob     = blobClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, overwrite: true);

        return blob.Uri.ToString();
    }

    private static UserProfileDto MapToDto(User u) => new()
    {
        UserId        = u.UserId,
        UserName      = u.UserName,
        FullName      = u.FullName,
        Email         = u.Email,
        Bio           = u.Bio,
        AvatarUrl     = u.AvatarUrl,
        IsPrivate     = u.IsPrivate,
        FollowerCount  = u.FollowerCount,
        FollowingCount = u.FollowingCount,
        PostCount      = u.PostCount,
        CreatedAt      = u.CreatedAt
    };
}
using ConnectSphere.Auth.API.DTOs;

namespace ConnectSphere.Auth.API.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync(int userId, string accessToken, string refreshToken);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<UserProfileDto> UpdateProfileAsync(
        int userId, UpdateProfileRequest request, IFormFile? avatar);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task TogglePrivacyAsync(int userId);
    Task<UserProfileDto?> GetProfileAsync(string userName);
    Task<List<UserProfileDto>> SearchUsersAsync(string query);
    Task<List<UserProfileDto>> GetSuggestionsAsync(int userId);
    Task DeactivateAccountAsync(int userId);

    Task<ExternalAuthResponse> ExternalLoginAsync(
        string email,
        string fullName,
        string userName,
        string provider,
        string? avatarUrl = null);

    Task UpdateFollowCountsAsync(int followerId, int followeeId, bool increment);
    Task UpdatePostCountAsync(int userId, bool increment);
    Task<UserProfileDto?> GetProfileByIdAsync(int userId);
    Task<List<UserProfileDto>> GetProfilesByIdsAsync(IEnumerable<int> userIds);
}
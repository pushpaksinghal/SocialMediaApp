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
}
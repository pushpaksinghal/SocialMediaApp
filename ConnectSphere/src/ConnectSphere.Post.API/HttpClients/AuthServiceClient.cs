using System.Net.Http.Json;

namespace ConnectSphere.Post.API.HttpClients;

public class AuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;

    public AuthServiceClient(
        HttpClient httpClient,
        ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    public async Task IncrementPostCountAsync(
        int userId, string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);

        var response = await _httpClient.PostAsync(
            $"/api/auth/increment-post/{userId}", null);

        if (!response.IsSuccessStatusCode)
            _logger.LogWarning(
                "Failed to increment post count for user {UserId}", userId);
    }

    public async Task DecrementPostCountAsync(
        int userId, string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);

        await _httpClient.PostAsync(
            $"/api/auth/decrement-post/{userId}", null);
    }

    /// <summary>
    /// Batch-fetch user profiles by their IDs from the Auth service.
    /// Returns a dictionary keyed by UserId for O(1) lookup.
    /// </summary>
    public async Task<Dictionary<int, AuthUserDto>> GetUsersByIdsAsync(
        IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, AuthUserDto>();

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/auth/users/batch", ids);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Batch user lookup failed: {Status}", response.StatusCode);
                return new Dictionary<int, AuthUserDto>();
            }

            var users = await response.Content
                .ReadFromJsonAsync<List<AuthUserDto>>()
                ?? new List<AuthUserDto>();

            return users.ToDictionary(u => u.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Batch user lookup exception: {Message}", ex.Message);
            return new Dictionary<int, AuthUserDto>();
        }
    }
}

/// <summary>Minimal projection of UserProfileDto returned by Auth API.</summary>
public record AuthUserDto(
    int    UserId,
    string UserName,
    string FullName,
    string? AvatarUrl);
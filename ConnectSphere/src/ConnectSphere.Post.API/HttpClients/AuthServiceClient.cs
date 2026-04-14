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
}
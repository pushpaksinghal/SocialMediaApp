using System.Net.Http.Json;

namespace ConnectSphere.Follow.API.HttpClients;

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

    public async Task UpdateFollowCountsAsync(
        int followerId,
        int followeeId,
        bool increment,
        string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);

        var payload = new
        {
            followerId = followerId,
            followeeId = followeeId,
            increment  = increment
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/update-counts", payload);

        if (!response.IsSuccessStatusCode)
            _logger.LogWarning(
                "Failed to update follow counts for users {FollowerId}/{FolloweeId}",
                followerId, followeeId);
    }
}
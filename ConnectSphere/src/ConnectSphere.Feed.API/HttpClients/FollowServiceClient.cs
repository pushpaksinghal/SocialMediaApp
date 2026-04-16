namespace ConnectSphere.Feed.API.HttpClients;

public class FollowServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FollowServiceClient> _logger;

    public FollowServiceClient(
        HttpClient httpClient,
        ILogger<FollowServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    public async Task<List<int>> GetFollowingIdsAsync(
        int userId, string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);

        var response = await _httpClient.GetAsync(
            $"/api/follows/ids/{userId}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Failed to get following IDs for user {UserId}", userId);
            return new List<int>();
        }

        var ids = await response.Content
            .ReadFromJsonAsync<List<int>>();

        return ids ?? new List<int>();
    }
}
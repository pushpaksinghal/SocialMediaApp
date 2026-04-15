using System.Net.Http.Json;

namespace ConnectSphere.Like.API.HttpClients;

public class PostServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PostServiceClient> _logger;

    public PostServiceClient(
        HttpClient httpClient,
        ILogger<PostServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    public async Task SyncLikeCountAsync(int postId, int count)
    {
        var response = await _httpClient.PostAsync(
            $"/api/posts/{postId}/sync-likes?count={count}", null);

        if (!response.IsSuccessStatusCode)
            _logger.LogWarning(
                "Failed to sync like count for post {PostId} to {Count}", postId, count);
    }
}

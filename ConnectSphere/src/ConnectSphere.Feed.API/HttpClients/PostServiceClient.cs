using ConnectSphere.Feed.API.DTOs;

namespace ConnectSphere.Feed.API.HttpClients;

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

    public async Task<List<FeedPostDto>> GetPostsByUserAsync(
        int userId, string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);

        var response = await _httpClient.GetAsync(
            $"/api/posts/user/{userId}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Failed to get posts for user {UserId}", userId);
            return new List<FeedPostDto>();
        }

        var posts = await response.Content
            .ReadFromJsonAsync<List<FeedPostDto>>();

        return posts ?? new List<FeedPostDto>();
    }

    public async Task<List<FeedPostDto>> GetPublicPostsAsync()
    {
        var response = await _httpClient.GetAsync("/api/posts/public");

        if (!response.IsSuccessStatusCode)
            return new List<FeedPostDto>();

        var posts = await response.Content
            .ReadFromJsonAsync<List<FeedPostDto>>();

        return posts ?? new List<FeedPostDto>();
    }

    public async Task<List<FeedPostDto>> GetTrendingPostsAsync()
    {
        var response = await _httpClient.GetAsync("/api/posts/trending");

        if (!response.IsSuccessStatusCode)
            return new List<FeedPostDto>();

        var posts = await response.Content
            .ReadFromJsonAsync<List<FeedPostDto>>();

        return posts ?? new List<FeedPostDto>();
    }
}
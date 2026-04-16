using System.Net.Http.Json;

namespace ConnectSphere.Comment.API.HttpClients;

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

    public async Task IncrementCommentCountAsync(int postId)
    {
        var response = await _httpClient.PostAsync(
            $"/api/posts/{postId}/increment-comment", null);

        if (!response.IsSuccessStatusCode)
            _logger.LogWarning(
                "Failed to increment comment count for post {PostId}", postId);
    }

    public async Task<int?> GetPostOwnerIdAsync(int postId)
    {
        var response = await _httpClient.GetAsync($"/api/posts/{postId}");

        if (!response.IsSuccessStatusCode) return null;

        var post = await response.Content
            .ReadFromJsonAsync<PostOwnerDto>();

        return post?.UserId;
    }
}

public class PostOwnerDto
{
    public int PostId { get; set; }
    public int UserId { get; set; }
}
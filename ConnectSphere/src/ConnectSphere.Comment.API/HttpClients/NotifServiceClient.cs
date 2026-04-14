using System.Net.Http.Json;

namespace ConnectSphere.Comment.API.HttpClients;

public class NotifServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotifServiceClient> _logger;

    public NotifServiceClient(
        HttpClient httpClient,
        ILogger<NotifServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    public async Task SendCommentNotifAsync(
        int actorId,
        int recipientId,
        int postId,
        string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);

        var payload = new
        {
            recipientId = recipientId,
            actorId     = actorId,
            type        = "NEW_COMMENT",
            message     = "commented on your post.",
            targetId    = postId,
            targetType  = "POST"
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/notifications/send", payload);

        if (!response.IsSuccessStatusCode)
            _logger.LogWarning(
                "Failed to send comment notification for post {PostId}", postId);
    }

    public async Task SendReplyNotifAsync(
        int actorId,
        int recipientId,
        int commentId,
        string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);

        var payload = new
        {
            recipientId = recipientId,
            actorId     = actorId,
            type        = "NEW_REPLY",
            message     = "replied to your comment.",
            targetId    = commentId,
            targetType  = "COMMENT"
        };

        await _httpClient.PostAsJsonAsync(
            "/api/notifications/send", payload);
    }
}
using System.Net.Http.Json;

namespace ConnectSphere.Like.API.HttpClients;

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

    public async Task SendLikeNotifAsync(
        int actorId,
        int recipientId,
        int targetId,
        string targetType,
        string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);

        var payload = new
        {
            recipientId = recipientId,
            actorId     = actorId,
            type        = targetType == "POST" ? "LIKE_POST" : "LIKE_COMMENT",
            message     = $"liked your {targetType.ToLower()}.",
            targetId    = targetId,
            targetType  = targetType
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/notifications/send", payload);

        if (!response.IsSuccessStatusCode)
            _logger.LogWarning(
                "Failed to send like notification to user {RecipientId}",
                recipientId);
    }
}
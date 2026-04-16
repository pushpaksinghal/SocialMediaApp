using System.Net.Http.Json;

namespace ConnectSphere.Follow.API.HttpClients;

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

    public async Task SendFollowNotifAsync(
        int actorId,
        int recipientId,
        bool isPending,
        string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);

        var payload = new
        {
            recipientId = recipientId,
            actorId     = actorId,
            type        = isPending ? "FOLLOW_REQUEST" : "NEW_FOLLOWER",
            message     = isPending
                            ? "sent you a follow request."
                            : "started following you.",
            targetId    = actorId,
            targetType  = "USER"
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/notifications/send", payload);

        if (!response.IsSuccessStatusCode)
            _logger.LogWarning(
                "Failed to send follow notification to user {RecipientId}",
                recipientId);
    }

    public async Task SendFollowAcceptedNotifAsync(
        int actorId,
        int recipientId,
        string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);

        var payload = new
        {
            recipientId = recipientId,
            actorId     = actorId,
            type        = "FOLLOW_ACCEPTED",
            message     = "accepted your follow request.",
            targetId    = actorId,
            targetType  = "USER"
        };

        await _httpClient.PostAsJsonAsync(
            "/api/notifications/send", payload);
    }
}
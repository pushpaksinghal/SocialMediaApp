using ConnectSphere.Notif.API.DTOs;

namespace ConnectSphere.Notif.API.Services;

public interface INotifService
{
    Task<NotifDto> SendAsync(SendNotifRequest request);
    Task SendLikeNotifAsync(int actorId, int recipientId, int targetId, string targetType);
    Task SendCommentNotifAsync(int actorId, int recipientId, int postId);
    Task SendFollowNotifAsync(int actorId, int recipientId, bool isPending);
    Task SendFollowAcceptedNotifAsync(int actorId, int recipientId);
    Task SendMentionNotifAsync(int actorId, int recipientId, int targetId, string targetType);
    Task<List<NotifDto>> GetNotificationsAsync(int userId, int page, int pageSize);
    Task<List<NotifDto>> GetUnreadAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllReadAsync(int userId);
    Task DeleteNotificationAsync(int notificationId, int userId);
    Task SendBulkAsync(BroadcastRequest request);
}
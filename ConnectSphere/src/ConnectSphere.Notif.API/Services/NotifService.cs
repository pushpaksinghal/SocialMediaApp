using ConnectSphere.Notif.API.Data;
using ConnectSphere.Notif.API.DTOs;
using ConnectSphere.Notif.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Notif.API.Services;

public class NotifService : INotifService
{
    private readonly NotifDbContext _db;
    private readonly ILogger<NotifService> _logger;

    public NotifService(NotifDbContext db, ILogger<NotifService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Send Generic Notification ────────────────────────────────────────────
    public async Task<NotifDto> SendAsync(SendNotifRequest request)
    {
        var notif = new Notification
        {
            RecipientId = request.RecipientId,
            ActorId     = request.ActorId,
            Type        = request.Type,
            Message     = request.Message,
            TargetId    = request.TargetId,
            TargetType  = request.TargetType,
            CreatedAt   = DateTime.UtcNow
        };

        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync();

        return MapToDto(notif);
    }

    // ── Send Like Notification ───────────────────────────────────────────────
    public async Task SendLikeNotifAsync(
        int actorId, int recipientId, int targetId, string targetType)
    {
        // Don't notify yourself
        if (actorId == recipientId) return;

        var type    = targetType == "POST" ? "LIKE_POST" : "LIKE_COMMENT";
        var message = $"liked your {targetType.ToLower()}.";

        await SendAsync(new SendNotifRequest
        {
            RecipientId = recipientId,
            ActorId     = actorId,
            Type        = type,
            Message     = message,
            TargetId    = targetId,
            TargetType  = targetType
        });
    }

    // ── Send Comment Notification ────────────────────────────────────────────
    public async Task SendCommentNotifAsync(
        int actorId, int recipientId, int postId)
    {
        if (actorId == recipientId) return;

        await SendAsync(new SendNotifRequest
        {
            RecipientId = recipientId,
            ActorId     = actorId,
            Type        = "NEW_COMMENT",
            Message     = "commented on your post.",
            TargetId    = postId,
            TargetType  = "POST"
        });
    }

    // ── Send Follow Notification ─────────────────────────────────────────────
    public async Task SendFollowNotifAsync(
        int actorId, int recipientId, bool isPending)
    {
        var type    = isPending ? "FOLLOW_REQUEST" : "NEW_FOLLOWER";
        var message = isPending
            ? "sent you a follow request."
            : "started following you.";

        await SendAsync(new SendNotifRequest
        {
            RecipientId = recipientId,
            ActorId     = actorId,
            Type        = type,
            Message     = message,
            TargetId    = actorId,
            TargetType  = "USER"
        });
    }

    // ── Send Follow Accepted Notification ────────────────────────────────────
    public async Task SendFollowAcceptedNotifAsync(
        int actorId, int recipientId)
    {
        await SendAsync(new SendNotifRequest
        {
            RecipientId = recipientId,
            ActorId     = actorId,
            Type        = "FOLLOW_ACCEPTED",
            Message     = "accepted your follow request.",
            TargetId    = actorId,
            TargetType  = "USER"
        });
    }

    // ── Send Mention Notification ────────────────────────────────────────────
    public async Task SendMentionNotifAsync(
        int actorId, int recipientId, int targetId, string targetType)
    {
        if (actorId == recipientId) return;

        await SendAsync(new SendNotifRequest
        {
            RecipientId = recipientId,
            ActorId     = actorId,
            Type        = "MENTION",
            Message     = $"mentioned you in a {targetType.ToLower()}.",
            TargetId    = targetId,
            TargetType  = targetType
        });
    }

    // ── Get Notifications (paginated) ────────────────────────────────────────
    public async Task<List<NotifDto>> GetNotificationsAsync(
        int userId, int page, int pageSize)
    {
        var notifications = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    // ── Get Unread ───────────────────────────────────────────────────────────
    public async Task<List<NotifDto>> GetUnreadAsync(int userId)
    {
        var notifications = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    // ── Get Unread Count ─────────────────────────────────────────────────────
    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.Notifications.CountAsync(n =>
            n.RecipientId == userId && !n.IsRead);
    }

    // ── Mark As Read ─────────────────────────────────────────────────────────
    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        await _db.Notifications
            .Where(n => n.NotificationId == notificationId &&
                        n.RecipientId    == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    // ── Mark All Read ────────────────────────────────────────────────────────
    public async Task MarkAllReadAsync(int userId)
    {
        await _db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    // ── Delete Notification ──────────────────────────────────────────────────
    public async Task DeleteNotificationAsync(int notificationId, int userId)
    {
        var notif = await _db.Notifications.FirstOrDefaultAsync(n =>
            n.NotificationId == notificationId &&
            n.RecipientId    == userId)
            ?? throw new KeyNotFoundException("Notification not found.");

        _db.Notifications.Remove(notif);
        await _db.SaveChangesAsync();
    }

    // ── Send Bulk (Admin broadcast) ──────────────────────────────────────────
    public async Task SendBulkAsync(BroadcastRequest request)
    {
        var notifications = request.UserIds.Select(userId => new Notification
        {
            RecipientId = userId,
            ActorId     = 0, // 0 = system/admin
            Type        = "PLATFORM",
            Message     = $"{request.Title}: {request.Message}",
            CreatedAt   = DateTime.UtcNow
        }).ToList();

        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Admin broadcast sent to {Count} users", notifications.Count);
    }

    // ── Helper ───────────────────────────────────────────────────────────────
    private static NotifDto MapToDto(Notification n) => new()
    {
        NotificationId = n.NotificationId,
        RecipientId    = n.RecipientId,
        ActorId        = n.ActorId,
        Type           = n.Type,
        Message        = n.Message,
        TargetId       = n.TargetId,
        TargetType     = n.TargetType,
        IsRead         = n.IsRead,
        CreatedAt      = n.CreatedAt
    };
}
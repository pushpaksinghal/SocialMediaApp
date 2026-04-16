using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Notif.API.Models;

[Index(nameof(RecipientId), nameof(IsRead))]
public class Notification
{
    public int NotificationId { get; set; }

    public int RecipientId { get; set; }

    public int ActorId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public int? TargetId { get; set; }

    public string? TargetType { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
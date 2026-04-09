using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Feed.API.Models;

[Index(nameof(UserId), nameof(CreatedAt))]
public class FeedItem
{
    public int FeedItemId { get; set; }

    public int UserId { get; set; }

    public int PostId { get; set; }

    public int ActorId { get; set; }

    public decimal Score { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }
}
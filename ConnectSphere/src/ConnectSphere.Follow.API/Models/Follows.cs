using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Follow.API.Models;

[Index(nameof(FollowerId), nameof(FolloweeId), IsUnique = true)]
[Index(nameof(FolloweeId))]
public class Follows
{
    public int FollowId { get; set; }

    public int FollowerId { get; set; }

    public int FolloweeId { get; set; }

    // PENDING / ACCEPTED / REJECTED
    public string Status { get; set; } = "PENDING";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
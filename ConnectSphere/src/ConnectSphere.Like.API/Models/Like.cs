using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Like.API.Models;

[Index(nameof(UserId), nameof(TargetId), nameof(TargetType), IsUnique = true)]
public class Like
{
    public int LikeId { get; set; }

    public int UserId { get; set; }

    public int TargetId { get; set; }

    public string TargetType { get; set; } = string.Empty; // POST or COMMENT

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
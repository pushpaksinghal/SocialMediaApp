using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Post.API.Models;

[Index(nameof(UserId), nameof(CreatedAt))]
public class Post
{
    public int PostId { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? MediaUrl { get; set; }

    [MaxLength(10)]
    public string? MediaType { get; set; }

    [MaxLength(12)]
    public string Visibility { get; set; } = "PUBLIC";

    [MaxLength(1024)]
    public string? Hashtags { get; set; }

    public int LikeCount { get; set; } = 0;

    public int CommentCount { get; set; } = 0;

    public int ShareCount { get; set; } = 0;

    public bool IsDeleted { get; set; } = false;

    public int? OriginalPostId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
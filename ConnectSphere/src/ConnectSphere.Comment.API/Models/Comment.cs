using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Comment.API.Models;

[Index(nameof(PostId), nameof(ParentCommentId))]
public class Comment
{
    public int CommentId { get; set; }

    public int PostId { get; set; }

    public int UserId { get; set; }

    public int? ParentCommentId { get; set; }

    public string Content { get; set; } = string.Empty;

    public int LikeCount { get; set; } = 0;

    public int ReplyCount { get; set; } = 0;

    public bool IsDeleted { get; set; } = false;

    public bool IsEdited { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EditedAt { get; set; }
}
namespace ConnectSphere.Post.API.DTOs;

/// <summary>
/// Data transfer object representing a post.
/// </summary>
public class PostDto
{
    /// <summary>The unique identifier for the post.</summary>
    public int PostId { get; set; }
    /// <summary>The ID of the user who created the post.</summary>
    public int UserId { get; set; }
    /// <summary>The text content of the post.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>URL to any media (image/video) attached to the post.</summary>
    public string? MediaUrl { get; set; }
    /// <summary>The type of media (IMAGE, VIDEO, etc.).</summary>
    public string? MediaType { get; set; }
    /// <summary>The visibility of the post (PUBLIC, PRIVATE).</summary>
    public string Visibility { get; set; } = string.Empty;
    /// <summary>Space-separated list of hashtags.</summary>
    public string? Hashtags { get; set; }
    /// <summary>Total number of likes on this post.</summary>
    public int LikeCount { get; set; }
    /// <summary>Total number of comments on this post.</summary>
    public int CommentCount { get; set; }
    /// <summary>Total number of times this post has been shared.</summary>
    public int ShareCount { get; set; }
    /// <summary>Whether the post has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }
    /// <summary>If this is a share, the ID of the original post.</summary>
    public int? OriginalPostId { get; set; }
    /// <summary>The timestamp when the post was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The timestamp when the post was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }

    // ── Enriched fields (populated by the service layer) ──────────
    /// <summary>Enriched information about the author (populated by service layer).</summary>
    public PostAuthorDto? Author { get; set; }
    /// <summary>Indicates if the currently logged-in user has liked this post.</summary>
    public bool IsLiked { get; set; }
}

/// <summary>
/// Brief information about a post's author.
/// </summary>
public class PostAuthorDto
{
    /// <summary>The author's unique handle/username.</summary>
    public string UserName { get; set; } = string.Empty;
    /// <summary>The author's display name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>URL to the author's profile picture.</summary>
    public string? AvatarUrl { get; set; }
}
namespace ConnectSphere.Post.API.DTOs;

public class PostDto
{
    public int PostId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public string Visibility { get; set; } = string.Empty;
    public string? Hashtags { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int ShareCount { get; set; }
    public bool IsDeleted { get; set; }
    public int? OriginalPostId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ── Enriched fields (populated by the service layer) ──────────
    public PostAuthorDto? Author { get; set; }
    public bool IsLiked { get; set; }
}

public class PostAuthorDto
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
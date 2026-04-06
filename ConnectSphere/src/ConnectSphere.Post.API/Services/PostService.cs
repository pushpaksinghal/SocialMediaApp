using Azure.Storage.Blobs;
using ConnectSphere.Post.API.Data;
using ConnectSphere.Post.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Post.API.Services;

public class PostService : IPostService
{
    private readonly PostDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PostService> _logger;

    public PostService(
        PostDbContext db,
        IConfiguration config,
        ILogger<PostService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    // ── Create Post ──────────────────────────────────────────────────────────
    public async Task<PostDto> CreatePostAsync(
        int userId, CreatePostRequest request, IFormFile? media)
    {
        var post = new Models.Post
        {
            UserId     = userId,
            Content    = request.Content,
            Visibility = request.Visibility,
            Hashtags   = request.Hashtags,
            CreatedAt  = DateTime.UtcNow
        };

        if (media is not null)
        {
            post.MediaUrl  = await UploadMediaAsync(userId, media);
            post.MediaType = ResolveMediaType(media.ContentType);
        }

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Post {PostId} created by user {UserId}", post.PostId, userId);

        return MapToDto(post);
    }

    // ── Get Post By ID ───────────────────────────────────────────────────────
    public async Task<PostDto?> GetPostByIdAsync(int postId, int? requestingUserId)
    {
        var post = await _db.Posts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PostId == postId && !p.IsDeleted);

        if (post is null) return null;

        // Guests can only see PUBLIC posts
        if (requestingUserId is null && post.Visibility != "PUBLIC")
            return null;

        return MapToDto(post);
    }

    // ── Get Posts By User ────────────────────────────────────────────────────
    public async Task<List<PostDto>> GetPostsByUserAsync(
        int userId, int? requestingUserId)
    {
        var query = _db.Posts
            .AsNoTracking()
            .Where(p => p.UserId == userId && !p.IsDeleted);

        // Guests see PUBLIC only; authenticated users see PUBLIC + FOLLOWERS
        if (requestingUserId is null)
            query = query.Where(p => p.Visibility == "PUBLIC");
        else if (requestingUserId != userId)
            query = query.Where(p =>
                p.Visibility == "PUBLIC" || p.Visibility == "FOLLOWERS");

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return posts.Select(MapToDto).ToList();
    }

    // ── Update Post ──────────────────────────────────────────────────────────
    public async Task<PostDto> UpdatePostAsync(
        int postId, int userId, UpdatePostRequest request)
    {
        var post = await _db.Posts
            .FirstOrDefaultAsync(p => p.PostId == postId && !p.IsDeleted)
            ?? throw new KeyNotFoundException("Post not found.");

        if (post.UserId != userId)
            throw new UnauthorizedAccessException("You can only edit your own posts.");

        post.Content    = request.Content;
        post.Visibility = request.Visibility;
        post.Hashtags   = request.Hashtags;
        post.UpdatedAt  = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(post);
    }

    // ── Soft Delete ──────────────────────────────────────────────────────────
    public async Task SoftDeletePostAsync(int postId, int userId)
    {
        var post = await _db.Posts
            .FirstOrDefaultAsync(p => p.PostId == postId && !p.IsDeleted)
            ?? throw new KeyNotFoundException("Post not found.");

        if (post.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own posts.");

        await _db.Posts
            .Where(p => p.PostId == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));
    }

    // ── Browse By Hashtag ────────────────────────────────────────────────────
    public async Task<List<PostDto>> GetByHashtagAsync(string tag)
    {
        var searchTag = tag.StartsWith("#") ? tag : $"#{tag}";

        var posts = await _db.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted &&
                p.Visibility == "PUBLIC" &&
                EF.Functions.ILike(p.Hashtags ?? "", $"%{searchTag}%"))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return posts.Select(MapToDto).ToList();
    }

    // ── Search Posts ─────────────────────────────────────────────────────────
    public async Task<List<PostDto>> SearchPostsAsync(string query)
    {
        var posts = await _db.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted &&
                p.Visibility == "PUBLIC" &&
                EF.Functions.ILike(p.Content, $"%{query}%"))
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToListAsync();

        return posts.Select(MapToDto).ToList();
    }

    // ── Trending Posts ───────────────────────────────────────────────────────
    public async Task<List<PostDto>> GetTrendingPostsAsync()
    {
        var since = DateTime.UtcNow.AddHours(-24);

        var posts = await _db.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted &&
                p.Visibility == "PUBLIC" &&
                p.CreatedAt >= since)
            .OrderByDescending(p =>
                p.LikeCount * 3 + p.CommentCount * 2 + p.ShareCount)
            .Take(20)
            .ToListAsync();

        return posts.Select(MapToDto).ToList();
    }

    // ── Share / Repost ───────────────────────────────────────────────────────
    public async Task<PostDto> SharePostAsync(int originalPostId, int userId)
    {
        var original = await _db.Posts
            .FirstOrDefaultAsync(p => p.PostId == originalPostId && !p.IsDeleted)
            ?? throw new KeyNotFoundException("Original post not found.");

        // Increment share count on original atomically
        await _db.Posts
            .Where(p => p.PostId == originalPostId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                p => p.ShareCount, p => p.ShareCount + 1));

        var repost = new Models.Post
        {
            UserId         = userId,
            Content        = original.Content,
            Visibility     = "PUBLIC",
            Hashtags       = original.Hashtags,
            OriginalPostId = originalPostId,
            CreatedAt      = DateTime.UtcNow
        };

        _db.Posts.Add(repost);
        await _db.SaveChangesAsync();

        return MapToDto(repost);
    }

    // ── Public Feed ──────────────────────────────────────────────────────────
    public async Task<List<PostDto>> GetPublicFeedAsync()
    {
        var posts = await _db.Posts
            .AsNoTracking()
            .Where(p => p.Visibility == "PUBLIC" && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .ToListAsync();

        return posts.Select(MapToDto).ToList();
    }

    // ── Increment Comment Count (called by Comment service) ──────────────────
    public async Task IncrementCommentCountAsync(int postId)
    {
        await _db.Posts
            .Where(p => p.PostId == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                p => p.CommentCount, p => p.CommentCount + 1));
    }

    // ── Admin Delete ─────────────────────────────────────────────────────────
    public async Task AdminDeletePostAsync(int postId)
    {
        await _db.Posts
            .Where(p => p.PostId == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));

        _logger.LogWarning(
            "Admin soft-deleted post {PostId}", postId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private async Task<string> UploadMediaAsync(int userId, IFormFile file)
    {
        var connStr   = _config["AzureBlob:ConnectionString"]!;
        var container = _config["AzureBlob:ContainerName"]!;
        var client    = new BlobContainerClient(connStr, container);

        await client.CreateIfNotExistsAsync();

        var blobName = $"posts/{userId}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var blob     = client.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, overwrite: true);

        return blob.Uri.ToString();
    }

    private static string ResolveMediaType(string contentType) =>
        contentType switch
        {
            var c when c.StartsWith("image/gif")  => "GIF",
            var c when c.StartsWith("image/")     => "IMAGE",
            var c when c.StartsWith("video/")     => "VIDEO",
            _                                     => "IMAGE"
        };

    private static PostDto MapToDto(Models.Post p) => new()
    {
        PostId         = p.PostId,
        UserId         = p.UserId,
        Content        = p.Content,
        MediaUrl       = p.MediaUrl,
        MediaType      = p.MediaType,
        Visibility     = p.Visibility,
        Hashtags       = p.Hashtags,
        LikeCount      = p.LikeCount,
        CommentCount   = p.CommentCount,
        ShareCount     = p.ShareCount,
        IsDeleted      = p.IsDeleted,
        OriginalPostId = p.OriginalPostId,
        CreatedAt      = p.CreatedAt,
        UpdatedAt      = p.UpdatedAt
    };
}
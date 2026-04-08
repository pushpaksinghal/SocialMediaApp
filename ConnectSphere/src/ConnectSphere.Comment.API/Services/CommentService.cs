using ConnectSphere.Comment.API.Data;
using ConnectSphere.Comment.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Comment.API.Services;

public class CommentService : ICommentService
{
    private readonly CommentDbContext _db;
    private readonly ILogger<CommentService> _logger;

    public CommentService(CommentDbContext db, ILogger<CommentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Add Comment ──────────────────────────────────────────────────────────
    public async Task<CommentDto> AddCommentAsync(
        int userId, AddCommentRequest request)
    {
        var comment = new Models.Comment
        {
            PostId          = request.PostId,
            UserId          = userId,
            ParentCommentId = request.ParentCommentId,
            Content         = request.Content,
            CreatedAt       = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        // If this is a reply, increment parent ReplyCount
        if (request.ParentCommentId.HasValue)
        {
            await _db.Comments
                .Where(c => c.CommentId == request.ParentCommentId.Value)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    c => c.ReplyCount, c => c.ReplyCount + 1));
        }

        _logger.LogInformation(
            "Comment {CommentId} added by user {UserId} on post {PostId}",
            comment.CommentId, userId, request.PostId);

        return MapToDto(comment);
    }

    // ── Get Comment By ID ────────────────────────────────────────────────────
    public async Task<CommentDto?> GetCommentByIdAsync(int commentId)
    {
        var comment = await _db.Comments
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CommentId == commentId);

        return comment is null ? null : MapToDto(comment);
    }

    // ── Get All Comments By Post ─────────────────────────────────────────────
    public async Task<List<CommentDto>> GetByPostAsync(int postId)
    {
        var comments = await _db.Comments
            .AsNoTracking()
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(MapToDto).ToList();
    }

    // ── Get Top Level Comments ───────────────────────────────────────────────
    public async Task<List<CommentDto>> GetTopLevelAsync(int postId)
    {
        var comments = await _db.Comments
            .AsNoTracking()
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(MapToDto).ToList();
    }

    // ── Get Replies ──────────────────────────────────────────────────────────
    public async Task<List<CommentDto>> GetRepliesAsync(int commentId)
    {
        var replies = await _db.Comments
            .AsNoTracking()
            .Where(c => c.ParentCommentId == commentId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return replies.Select(MapToDto).ToList();
    }

    // ── Edit Comment ─────────────────────────────────────────────────────────
    public async Task<CommentDto> EditCommentAsync(
        int commentId, int userId, EditCommentRequest request)
    {
        var comment = await _db.Comments
            .FirstOrDefaultAsync(c => c.CommentId == commentId)
            ?? throw new KeyNotFoundException("Comment not found.");

        if (comment.UserId != userId)
            throw new UnauthorizedAccessException(
                "You can only edit your own comments.");

        await _db.Comments
            .Where(c => c.CommentId == commentId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Content,  request.Content)
                .SetProperty(c => c.IsEdited, true)
                .SetProperty(c => c.EditedAt, DateTime.UtcNow));

        comment.Content  = request.Content;
        comment.IsEdited = true;
        comment.EditedAt = DateTime.UtcNow;

        return MapToDto(comment);
    }

    // ── Soft Delete Comment ──────────────────────────────────────────────────
    public async Task DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _db.Comments
            .FirstOrDefaultAsync(c => c.CommentId == commentId)
            ?? throw new KeyNotFoundException("Comment not found.");

        if (comment.UserId != userId)
            throw new UnauthorizedAccessException(
                "You can only delete your own comments.");

        await _db.Comments
            .Where(c => c.CommentId == commentId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true));
    }

    // ── Get Comment Count ────────────────────────────────────────────────────
    public async Task<int> GetCommentCountAsync(int postId)
    {
        return await _db.Comments.CountAsync(c =>
            c.PostId == postId && !c.IsDeleted);
    }

    // ── Admin Delete ─────────────────────────────────────────────────────────
    public async Task AdminDeleteCommentAsync(int commentId)
    {
        await _db.Comments
            .Where(c => c.CommentId == commentId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true));

        _logger.LogWarning("Admin soft-deleted comment {CommentId}", commentId);
    }

    // ── Helper ───────────────────────────────────────────────────────────────
    private static CommentDto MapToDto(Models.Comment c) => new()
    {
        CommentId       = c.CommentId,
        PostId          = c.PostId,
        UserId          = c.UserId,
        ParentCommentId = c.ParentCommentId,
        // Show placeholder for soft deleted comments
        Content         = c.IsDeleted
                            ? "This comment was deleted."
                            : c.Content,
        LikeCount       = c.LikeCount,
        ReplyCount      = c.ReplyCount,
        IsDeleted       = c.IsDeleted,
        IsEdited        = c.IsEdited,
        CreatedAt       = c.CreatedAt,
        EditedAt        = c.EditedAt
    };
}
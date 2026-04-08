using ConnectSphere.Like.API.Data;
using ConnectSphere.Like.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Like.API.Services;

public class LikeService : ILikeService
{
    private readonly LikeDbContext _db;
    private readonly ILogger<LikeService> _logger;

    public LikeService(LikeDbContext db, ILogger<LikeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Toggle Like ──────────────────────────────────────────────────────────
    public async Task<ToggleLikeResponse> ToggleLikeAsync(
        int userId, ToggleLikeRequest request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var existing = await _db.Likes.FirstOrDefaultAsync(l =>
                l.UserId     == userId &&
                l.TargetId   == request.TargetId &&
                l.TargetType == request.TargetType);

            bool liked;

            if (existing is null)
            {
                // Add like
                _db.Likes.Add(new Models.Like
                {
                    UserId     = userId,
                    TargetId   = request.TargetId,
                    TargetType = request.TargetType,
                    CreatedAt  = DateTime.UtcNow
                });
                liked = true;
                _logger.LogInformation(
                    "User {UserId} liked {TargetType} {TargetId}",
                    userId, request.TargetType, request.TargetId);
            }
            else
            {
                // Remove like
                _db.Likes.Remove(existing);
                liked = false;
                _logger.LogInformation(
                    "User {UserId} unliked {TargetType} {TargetId}",
                    userId, request.TargetType, request.TargetId);
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Get updated count
            var count = await GetLikeCountAsync(
                request.TargetId, request.TargetType);

            return new ToggleLikeResponse { Liked = liked, LikeCount = count };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ── Get Likes By Target ──────────────────────────────────────────────────
    public async Task<List<LikeDto>> GetLikesByTargetAsync(
        int targetId, string targetType)
    {
        var likes = await _db.Likes
            .AsNoTracking()
            .Where(l => l.TargetId == targetId && l.TargetType == targetType)
            .ToListAsync();

        return likes.Select(MapToDto).ToList();
    }

    // ── Get Likes By User ────────────────────────────────────────────────────
    public async Task<List<LikeDto>> GetLikesByUserAsync(int userId)
    {
        var likes = await _db.Likes
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return likes.Select(MapToDto).ToList();
    }

    // ── Get Like Count ───────────────────────────────────────────────────────
    public async Task<int> GetLikeCountAsync(int targetId, string targetType)
    {
        return await _db.Likes.CountAsync(l =>
            l.TargetId == targetId && l.TargetType == targetType);
    }

    // ── Has User Liked ───────────────────────────────────────────────────────
    public async Task<bool> HasUserLikedAsync(
        int userId, int targetId, string targetType)
    {
        return await _db.Likes.AnyAsync(l =>
            l.UserId     == userId &&
            l.TargetId   == targetId &&
            l.TargetType == targetType);
    }

    // ── Get Likers For Post ──────────────────────────────────────────────────
    public async Task<List<int>> GetLikersForPostAsync(int postId)
    {
        return await _db.Likes
            .AsNoTracking()
            .Where(l => l.TargetId == postId && l.TargetType == "POST")
            .Select(l => l.UserId)
            .ToListAsync();
    }

    // ── Get Liked Posts By User ──────────────────────────────────────────────
    public async Task<List<int>> GetLikedPostsByUserAsync(int userId)
    {
        return await _db.Likes
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.TargetType == "POST")
            .Select(l => l.TargetId)
            .ToListAsync();
    }

    // ── Helper ───────────────────────────────────────────────────────────────
    private static LikeDto MapToDto(Models.Like l) => new()
    {
        LikeId     = l.LikeId,
        UserId     = l.UserId,
        TargetId   = l.TargetId,
        TargetType = l.TargetType,
        CreatedAt  = l.CreatedAt
    };
}
using ConnectSphere.Like.API.Data;
using ConnectSphere.Like.API.DTOs;
using ConnectSphere.Like.API.HttpClients;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Like.API.Services;

public class LikeService : ILikeService
{
    private readonly LikeDbContext _db;

    private readonly NotifServiceClient _notifClient;
    private readonly PostServiceClient _postClient;
    private readonly ILogger<LikeService> _logger;

    public LikeService(LikeDbContext db,
        NotifServiceClient notifClient,
        PostServiceClient postClient,
        ILogger<LikeService> logger)
    {
        _db = db;
        _notifClient= notifClient;
        _postClient = postClient;
        _logger = logger;
    }

    // ── Toggle Like ──────────────────────────────────────────────────────────
    public async Task<ToggleLikeResponse> ToggleLikeAsync(
        int userId, ToggleLikeRequest request, string accessToken)
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
                _db.Likes.Add(new Models.Like
                {
                    UserId     = userId,
                    TargetId   = request.TargetId,
                    TargetType = request.TargetType,
                    CreatedAt  = DateTime.UtcNow
                });
                liked = true;
            }
            else
            {
                _db.Likes.Remove(existing);
                liked = false;
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            var count = await GetLikeCountAsync(
                request.TargetId, request.TargetType);

            // Sync like count to the target service (e.g. Post API)
            if (request.TargetType == "POST")
            {
                _ = _postClient.SyncLikeCountAsync(request.TargetId, count);
            }

            // Send notification only when liking not unliking
            if (liked)
            {
                // Fire and forget — don't block the response
                _ = _notifClient.SendLikeNotifAsync(
                    actorId     : userId,
                    recipientId : request.TargetId, // will be refined later
                    targetId    : request.TargetId,
                    targetType  : request.TargetType,
                    accessToken : accessToken);
            }

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
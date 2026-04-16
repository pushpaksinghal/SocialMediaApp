using ConnectSphere.Follow.API.Data;
using ConnectSphere.Follow.API.DTOs;
using ConnectSphere.Follow.API.HttpClients;
using ConnectSphere.Follow.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Follow.API.Services;

public class FollowService : IFollowService
{
    private readonly FollowDbContext _db;

    private readonly NotifServiceClient _notifClient;
    private readonly AuthServiceClient _authClient;
    private readonly ILogger<FollowService> _logger;

    public FollowService(FollowDbContext db,NotifServiceClient notifClient,
        AuthServiceClient authClient, ILogger<FollowService> logger)
    {
        _db = db;
        _notifClient = notifClient;
        _authClient  = authClient;
        _logger = logger;
    }

    // ── Follow User ──────────────────────────────────────────────────────────
    public async Task<FollowDto> FollowUserAsync(
        int followerId, FollowRequest request, string accessToken)
    {
        var existing = await _db.Follows.FirstOrDefaultAsync(f =>
            f.FollowerId == followerId &&
            f.FolloweeId == request.FolloweeId);

        if (existing is not null)
            throw new InvalidOperationException(
                "Already following or request pending.");

        var status = request.IsPrivate ? "PENDING" : "ACCEPTED";

        var follow = new Follows
        {
            FollowerId = followerId,
            FolloweeId = request.FolloweeId,
            Status     = status,
            CreatedAt  = DateTime.UtcNow
        };

        _db.Follows.Add(follow);
        await _db.SaveChangesAsync();

        // If public account — update counts immediately
        if (status == "ACCEPTED")
        {
            _ = _authClient.UpdateFollowCountsAsync(
                followerId, request.FolloweeId, true, accessToken);
        }

        // Send notification
        _ = _notifClient.SendFollowNotifAsync(
            actorId     : followerId,
            recipientId : request.FolloweeId,
            isPending   : status == "PENDING",
            accessToken : accessToken);

        return MapToDto(follow);
    }

    // ── Accept Follow Request ────────────────────────────────────────────────
    public async Task<FollowDto> AcceptFollowRequestAsync(
        int followId, int userId, string accessToken)
    {
        var follow = await _db.Follows.FirstOrDefaultAsync(f =>
            f.FollowId == followId && f.FolloweeId == userId)
            ?? throw new KeyNotFoundException("Follow request not found.");

        if (follow.Status != "PENDING")
            throw new InvalidOperationException("Request is not pending.");

        await _db.Follows
            .Where(f => f.FollowId == followId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                f => f.Status, "ACCEPTED"));

        follow.Status = "ACCEPTED";

        // Update counts now that request is accepted
        _ = _authClient.UpdateFollowCountsAsync(
            follow.FollowerId, follow.FolloweeId, true, accessToken);

        // Notify the follower their request was accepted
        _ = _notifClient.SendFollowAcceptedNotifAsync(
            actorId     : userId,
            recipientId : follow.FollowerId,
            accessToken : accessToken);

        return MapToDto(follow);
    }

    // ── Reject Follow Request ────────────────────────────────────────────────
    public async Task<FollowDto> RejectFollowRequestAsync(
        int followId, int userId)
    {
        var follow = await _db.Follows.FirstOrDefaultAsync(f =>
            f.FollowId == followId && f.FolloweeId == userId)
            ?? throw new KeyNotFoundException("Follow request not found.");

        _db.Follows.Remove(follow);
        await _db.SaveChangesAsync();

        return MapToDto(follow);
    }

    // ── Unfollow ─────────────────────────────────────────────────────────────
    public async Task UnfollowUserAsync(
        int followerId, int followeeId, string accessToken)
    {
        var follow = await _db.Follows.FirstOrDefaultAsync(f =>
            f.FollowerId == followerId && f.FolloweeId == followeeId)
            ?? throw new KeyNotFoundException("Follow relationship not found.");

        _db.Follows.Remove(follow);
        await _db.SaveChangesAsync();

        // Decrement counts
        _ = _authClient.UpdateFollowCountsAsync(
            followerId, followeeId, false, accessToken);
    }

    // ── Get Followers ────────────────────────────────────────────────────────
    public async Task<List<FollowDto>> GetFollowersAsync(int userId)
    {
        var followers = await _db.Follows
            .AsNoTracking()
            .Where(f => f.FolloweeId == userId && f.Status == "ACCEPTED")
            .ToListAsync();

        return followers.Select(MapToDto).ToList();
    }

    // ── Get Following ────────────────────────────────────────────────────────
    public async Task<List<FollowDto>> GetFollowingAsync(int userId)
    {
        var following = await _db.Follows
            .AsNoTracking()
            .Where(f => f.FollowerId == userId && f.Status == "ACCEPTED")
            .ToListAsync();

        return following.Select(MapToDto).ToList();
    }

    // ── Get Pending Requests ─────────────────────────────────────────────────
    public async Task<List<FollowDto>> GetPendingRequestsAsync(int userId)
    {
        var pending = await _db.Follows
            .AsNoTracking()
            .Where(f => f.FolloweeId == userId && f.Status == "PENDING")
            .ToListAsync();

        return pending.Select(MapToDto).ToList();
    }

    // ── Is Following ─────────────────────────────────────────────────────────
    public async Task<bool> IsFollowingAsync(int followerId, int followeeId)
    {
        return await _db.Follows.AnyAsync(f =>
            f.FollowerId == followerId &&
            f.FolloweeId == followeeId &&
            f.Status     == "ACCEPTED");
    }

    // ── Get Following IDs (used by Feed service) ─────────────────────────────
    public async Task<List<int>> GetFollowingIdsAsync(int userId)
    {
        return await _db.Follows
            .AsNoTracking()
            .Where(f => f.FollowerId == userId && f.Status == "ACCEPTED")
            .Select(f => f.FolloweeId)
            .ToListAsync();
    }

    // ── Get Mutual Followers ─────────────────────────────────────────────────
    public async Task<List<int>> GetMutualFollowersAsync(
        int userIdA, int userIdB)
    {
        var aFollowers = await _db.Follows
            .AsNoTracking()
            .Where(f => f.FolloweeId == userIdA && f.Status == "ACCEPTED")
            .Select(f => f.FollowerId)
            .ToListAsync();

        var bFollowers = await _db.Follows
            .AsNoTracking()
            .Where(f => f.FolloweeId == userIdB && f.Status == "ACCEPTED")
            .Select(f => f.FollowerId)
            .ToListAsync();

        return aFollowers.Intersect(bFollowers).ToList();
    }

    // ── Helper ───────────────────────────────────────────────────────────────
    private static FollowDto MapToDto(Follows f) => new()
    {
        FollowId   = f.FollowId,
        FollowerId = f.FollowerId,
        FolloweeId = f.FolloweeId,
        Status     = f.Status,
        CreatedAt  = f.CreatedAt
    };
}
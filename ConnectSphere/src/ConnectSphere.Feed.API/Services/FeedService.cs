using System.Text.Json;
using ConnectSphere.Feed.API.DTOs;
using ConnectSphere.Feed.API.HttpClients;
using Microsoft.Extensions.Caching.Distributed;

namespace ConnectSphere.Feed.API.Services;

public class FeedService : IFeedService
{
    private readonly IDistributedCache _cache;
    private readonly FollowServiceClient _followClient;
    private readonly PostServiceClient _postClient;
    private readonly ILogger<FeedService> _logger;

    private static readonly DistributedCacheEntryOptions CacheOptions =
        new DistributedCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));

    public FeedService(
        IDistributedCache cache,
        FollowServiceClient followClient,
        PostServiceClient postClient,
        ILogger<FeedService> logger)
    {
        _cache         = cache;
        _followClient  = followClient;
        _postClient    = postClient;
        _logger        = logger;
    }

    // ── Get Home Feed (cache-aside) ──────────────────────────────────────────
    public async Task<FeedResponseDto> GetHomeFeedAsync(
        int userId, string accessToken, int page, int pageSize)
    {
        var cacheKey = $"feed:{userId}:p{page}";

        // 1. Check Redis cache first
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            _logger.LogInformation(
                "Cache HIT for feed:{UserId}", userId);

            var cachedPosts = JsonSerializer
                .Deserialize<List<FeedPostDto>>(cached)!;

            return new FeedResponseDto
            {
                Posts     = cachedPosts,
                Page      = page,
                PageSize  = pageSize,
                FromCache = true
            };
        }

        _logger.LogInformation(
            "Cache MISS for feed:{UserId} — querying services", userId);

        // 2. Cache miss — get following IDs from Follow service
        var followingIds = await _followClient
            .GetFollowingIdsAsync(userId, accessToken);

        if (!followingIds.Any())
        {
            return new FeedResponseDto
            {
                Posts     = new List<FeedPostDto>(),
                Page      = page,
                PageSize  = pageSize,
                FromCache = false
            };
        }

        // 3. Get posts from each followed user
        var allPosts = new List<FeedPostDto>();

        foreach (var followingId in followingIds)
        {
            var posts = await _postClient
                .GetPostsByUserAsync(followingId, accessToken);
            allPosts.AddRange(posts);
        }

        // 4. Sort by most recent and paginate
        var pagedPosts = allPosts
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // 5. Store in Redis with 5-min sliding TTL
        var serialized = JsonSerializer.Serialize(pagedPosts);
        await _cache.SetStringAsync(cacheKey, serialized, CacheOptions);

        return new FeedResponseDto
        {
            Posts     = pagedPosts,
            Page      = page,
            PageSize  = pageSize,
            FromCache = false
        };
    }

    // ── Get Explore Feed ─────────────────────────────────────────────────────
    public async Task<List<FeedPostDto>> GetExploreFeedAsync(
        int userId, string accessToken)
    {
        // Get IDs the user already follows
        var followingIds = await _followClient
            .GetFollowingIdsAsync(userId, accessToken);

        // Get all public posts
        var publicPosts = await _postClient.GetPublicPostsAsync();

        // Filter out posts from followed users and own posts
        // then rank by engagement
        var explorePosts = publicPosts
            .Where(p => !followingIds.Contains(p.UserId) &&
                        p.UserId != userId)
            .OrderByDescending(p =>
                p.LikeCount + p.CommentCount)
            .Take(20)
            .ToList();

        return explorePosts;
    }

    // ── Get User Timeline ────────────────────────────────────────────────────
    public async Task<List<FeedPostDto>> GetUserTimelineAsync(int userId)
    {
        var posts = await _postClient.GetPublicPostsAsync();

        return posts
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();
    }

    // ── Get Trending Hashtags ────────────────────────────────────────────────
    public async Task<List<TrendingHashtagDto>> GetTrendingHashtagsAsync(
        int topN)
    {
        var posts = await _postClient.GetPublicPostsAsync();

        var hashtags = posts
            .Where(p => !string.IsNullOrEmpty(p.Hashtags))
            .SelectMany(p => p.Hashtags!
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(h => h.Trim()))
            .GroupBy(h => h)
            .Select(g => new TrendingHashtagDto
            {
                Hashtag = g.Key,
                Count   = g.Count()
            })
            .OrderByDescending(h => h.Count)
            .Take(topN)
            .ToList();

        return hashtags;
    }

    // ── Invalidate Feed Cache ────────────────────────────────────────────────
    public async Task InvalidateFeedCacheAsync(int userId)
    {
        // Remove all pages for this user
        for (int page = 1; page <= 5; page++)
        {
            await _cache.RemoveAsync($"feed:{userId}:p{page}");
        }

        _logger.LogInformation(
            "Feed cache invalidated for user {UserId}", userId);
    }
}
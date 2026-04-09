using System.Security.Claims;
using ConnectSphere.Feed.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectSphere.Feed.API.Controllers;

[ApiController]
[Route("api/feed")]
public class FeedController : ControllerBase
{
    private readonly IFeedService _feedService;

    public FeedController(IFeedService feedService)
    {
        _feedService = feedService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string GetAccessToken() =>
        HttpContext.Request.Headers.Authorization
            .ToString()["Bearer ".Length..].Trim();

    // GET /api/feed/{userId}?page=1&size=20
    [Authorize]
    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetHomeFeed(
        int userId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        var feed = await _feedService.GetHomeFeedAsync(
            userId, GetAccessToken(), page, size);
        return Ok(feed);
    }

    // GET /api/feed/explore/{userId}
    [Authorize]
    [HttpGet("explore/{userId:int}")]
    public async Task<IActionResult> GetExploreFeed(int userId)
    {
        var posts = await _feedService.GetExploreFeedAsync(
            userId, GetAccessToken());
        return Ok(posts);
    }

    // GET /api/feed/timeline/{userId}
    [HttpGet("timeline/{userId:int}")]
    public async Task<IActionResult> GetTimeline(int userId)
    {
        var posts = await _feedService.GetUserTimelineAsync(userId);
        return Ok(posts);
    }

    // GET /api/feed/trending-hashtags?n=10
    [HttpGet("trending-hashtags")]
    public async Task<IActionResult> GetTrendingHashtags(
        [FromQuery] int n = 10)
    {
        var hashtags = await _feedService.GetTrendingHashtagsAsync(n);
        return Ok(hashtags);
    }

    // DELETE /api/feed/cache/{userId}
    [Authorize]
    [HttpDelete("cache/{userId:int}")]
    public async Task<IActionResult> InvalidateCache(int userId)
    {
        await _feedService.InvalidateFeedCacheAsync(userId);
        return Ok(new { message = "Feed cache invalidated." });
    }
}
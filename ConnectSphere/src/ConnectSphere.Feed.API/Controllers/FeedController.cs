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
    /// <summary>
    /// Retrieves the home feed for a specific user.
    /// </summary>
    /// <param name="userId">The User ID.</param>
    /// <param name="page">The page number for pagination.</param>
    /// <param name="size">The number of items per page.</param>
    /// <returns>A list of posts for the home feed.</returns>
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
    /// <summary>
    /// Retrieves the explore feed for a specific user.
    /// </summary>
    /// <param name="userId">The User ID.</param>
    /// <returns>A list of recommended posts.</returns>
    [Authorize]
    [HttpGet("explore/{userId:int}")]
    public async Task<IActionResult> GetExploreFeed(int userId)
    {
        var posts = await _feedService.GetExploreFeedAsync(
            userId, GetAccessToken());
        return Ok(posts);
    }

    // GET /api/feed/timeline/{userId}
    /// <summary>
    /// Retrieves the timeline of posts for a specific user.
    /// </summary>
    /// <param name="userId">The User ID.</param>
    /// <returns>A list of posts made by the user.</returns>
    [HttpGet("timeline/{userId:int}")]
    public async Task<IActionResult> GetTimeline(int userId)
    {
        var posts = await _feedService.GetUserTimelineAsync(userId);
        return Ok(posts);
    }

    // GET /api/feed/trending-hashtags?n=10
    /// <summary>
    /// Retrieves the most popular hashtags.
    /// </summary>
    /// <param name="n">The number of hashtags to return.</param>
    /// <returns>A list of trending hashtags.</returns>
    [HttpGet("trending-hashtags")]
    public async Task<IActionResult> GetTrendingHashtags(
        [FromQuery] int n = 10)
    {
        var hashtags = await _feedService.GetTrendingHashtagsAsync(n);
        return Ok(hashtags);
    }

    // DELETE /api/feed/cache/{userId}
    /// <summary>
    /// Invalidates the feed cache for a specific user.
    /// </summary>
    /// <param name="userId">The User ID.</param>
    /// <returns>A success message.</returns>
    [Authorize]
    [HttpDelete("cache/{userId:int}")]
    public async Task<IActionResult> InvalidateCache(int userId)
    {
        await _feedService.InvalidateFeedCacheAsync(userId);
        return Ok(new { message = "Feed cache invalidated." });
    }
}
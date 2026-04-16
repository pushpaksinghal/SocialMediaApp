using System.Security.Claims;
using ConnectSphere.Like.API.DTOs;
using ConnectSphere.Like.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectSphere.Like.Controllers;

[ApiController]
[Route("api/likes")]
public class LikeController : ControllerBase
{
    private readonly ILikeService _likeService;

    public LikeController(ILikeService likeService)
    {
        _likeService = likeService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST /api/likes/toggle
    [Authorize]
    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] ToggleLikeRequest request)
    {
        var accessToken = HttpContext.Request.Headers.Authorization
            .ToString()["Bearer ".Length..].Trim();

        var result = await _likeService.ToggleLikeAsync(
            GetUserId(), request, accessToken);

        return Ok(result);
    }

    // GET /api/likes/target/{id}/{type}
    [Authorize]
    [HttpGet("target/{id:int}/{type}")]
    public async Task<IActionResult> GetByTarget(int id, string type)
    {
        var likes = await _likeService.GetLikesByTargetAsync(id, type.ToUpper());
        return Ok(likes);
    }

    // GET /api/likes/user/{userId}
    [Authorize]
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var likes = await _likeService.GetLikesByUserAsync(userId);
        return Ok(likes);
    }

    // GET /api/likes/count/{id}/{type}
    [Authorize]
    [HttpGet("count/{id:int}/{type}")]
    public async Task<IActionResult> GetCount(int id, string type)
    {
        var count = await _likeService.GetLikeCountAsync(id, type.ToUpper());
        return Ok(new { count });
    }

    // GET /api/likes/has/{userId}/{id}/{type}
    [Authorize]
    [HttpGet("has/{userId:int}/{id:int}/{type}")]
    public async Task<IActionResult> HasLiked(int userId, int id, string type)
    {
        var result = await _likeService.HasUserLikedAsync(
            userId, id, type.ToUpper());
        return Ok(new { liked = result });
    }

    // GET /api/likes/post/{postId}/likers
    [Authorize]
    [HttpGet("post/{postId:int}/likers")]
    public async Task<IActionResult> GetLikers(int postId)
    {
        var likers = await _likeService.GetLikersForPostAsync(postId);
        return Ok(likers);
    }

    // GET /api/likes/user/{userId}/posts
    [Authorize]
    [HttpGet("user/{userId:int}/posts")]
    public async Task<IActionResult> GetLikedPosts(int userId)
    {
        var posts = await _likeService.GetLikedPostsByUserAsync(userId);
        return Ok(posts);
    }
}
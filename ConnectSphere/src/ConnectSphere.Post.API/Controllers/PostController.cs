using System.Security.Claims;
using ConnectSphere.Post.API.DTOs;
using ConnectSphere.Post.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectSphere.Post.API.Controllers;

[ApiController]
[Route("api/posts")]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;

    public PostController(IPostService postService)
    {
        _postService = postService;
    }

    private int? GetRequestingUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return value is null ? null : int.Parse(value);
    }

    private string GetAccessToken() =>
    HttpContext.Request.Headers.Authorization
        .ToString()["Bearer ".Length..].Trim();

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePost(
        [FromForm] CreatePostRequest request,
        IFormFile? media)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var post = await _postService.CreatePostAsync(
            userId, request, media, GetAccessToken());

        return StatusCode(201, post);
    }

    // GET /api/posts/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPost(int id)
    {
        var post = await _postService.GetPostByIdAsync(id, GetRequestingUserId());
        if (post is null)
            return NotFound(new { message = "Post not found." });

        return Ok(post);
    }

    // GET /api/posts/user/{userId}
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetPostsByUser(int userId)
    {
        var posts = await _postService.GetPostsByUserAsync(
            userId, GetRequestingUserId());

        return Ok(posts);
    }

    // PUT /api/posts/{id}
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePost(
        int id, [FromBody] UpdatePostRequest request)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var post = await _postService.UpdatePostAsync(id, userId, request);
            return Ok(post);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // DELETE /api/posts/{id}
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        try
        {
            await _postService.SoftDeletePostAsync(
                id, GetRequestingUserId()!.Value, GetAccessToken());
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // GET /api/posts/hashtag/{tag}
    [HttpGet("hashtag/{tag}")]
    public async Task<IActionResult> GetByHashtag(string tag)
    {
        var posts = await _postService.GetByHashtagAsync(tag);
        return Ok(posts);
    }

    // GET /api/posts/search?q=
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query cannot be empty." });

        var posts = await _postService.SearchPostsAsync(q);
        return Ok(posts);
    }

    // GET /api/posts/trending
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending()
    {
        var posts = await _postService.GetTrendingPostsAsync();
        return Ok(posts);
    }

    // POST /api/posts/share/{id}
    [Authorize]
    [HttpPost("share/{id:int}")]
    public async Task<IActionResult> SharePost(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var post = await _postService.SharePostAsync(id, userId);
            return StatusCode(201, post);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET /api/posts/public
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicFeed()
    {
        var posts = await _postService.GetPublicFeedAsync();
        return Ok(posts);
    }

    // DELETE /api/posts/admin/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/{id:int}")]
    public async Task<IActionResult> AdminDeletePost(int id)
    {
        await _postService.AdminDeletePostAsync(id);
        return NoContent();
    }
    [HttpPost("{id:int}/increment-comment")]
    public async Task<IActionResult> IncrementCommentCount(int id)
    {
        await _postService.IncrementCommentCountAsync(id);
        return Ok();
    }

    // POST /api/posts/{id}/sync-likes?count=
    [HttpPost("{id:int}/sync-likes")]
    public async Task<IActionResult> SyncLikeCount(int id, [FromQuery] int count)
    {
        await _postService.SyncLikeCountAsync(id, count);
        return Ok();
    }
}
using System.Security.Claims;
using ConnectSphere.Comment.API.DTOs;
using ConnectSphere.Comment.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectSphere.Comment.API.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST /api/comments
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddComment(
        [FromBody] AddCommentRequest request)
    {
        var accessToken = HttpContext.Request.Headers.Authorization
            .ToString()["Bearer ".Length..].Trim();

        var comment = await _commentService.AddCommentAsync(
            GetUserId(), request, accessToken);

        return StatusCode(201, comment);
    }

    // GET /api/comments/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetComment(int id)
    {
        var comment = await _commentService.GetCommentByIdAsync(id);
        if (comment is null)
            return NotFound(new { message = "Comment not found." });

        return Ok(comment);
    }

    // GET /api/comments/post/{id}
    [HttpGet("post/{id:int}")]
    public async Task<IActionResult> GetByPost(int id)
    {
        var comments = await _commentService.GetByPostAsync(id);
        return Ok(comments);
    }

    // GET /api/comments/toplevel/{postId}
    [HttpGet("toplevel/{postId:int}")]
    public async Task<IActionResult> GetTopLevel(int postId)
    {
        var comments = await _commentService.GetTopLevelAsync(postId);
        return Ok(comments);
    }

    // GET /api/comments/replies/{commentId}
    [HttpGet("replies/{commentId:int}")]
    public async Task<IActionResult> GetReplies(int commentId)
    {
        var replies = await _commentService.GetRepliesAsync(commentId);
        return Ok(replies);
    }

    // PUT /api/comments/{id}
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditComment(
        int id, [FromBody] EditCommentRequest request)
    {
        try
        {
            var comment = await _commentService.EditCommentAsync(
                id, GetUserId(), request);
            return Ok(comment);
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

    // DELETE /api/comments/{id}
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        try
        {
            await _commentService.DeleteCommentAsync(id, GetUserId());
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

    // GET /api/comments/count/{postId}
    [HttpGet("count/{postId:int}")]
    public async Task<IActionResult> GetCount(int postId)
    {
        var count = await _commentService.GetCommentCountAsync(postId);
        return Ok(new { count });
    }

    // DELETE /api/comments/admin/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/{id:int}")]
    public async Task<IActionResult> AdminDelete(int id)
    {
        await _commentService.AdminDeleteCommentAsync(id);
        return NoContent();
    }
}
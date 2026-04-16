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
    /// <summary>
    /// Adds a new comment to a post.
    /// </summary>
    /// <param name="request">The comment details (postId and content).</param>
    /// <returns>The created comment.</returns>
    /// <response code="201">Returns the newly created comment.</response>
    /// <response code="401">Unauthorized.</response>
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
    /// <summary>
    /// Retrieves a single comment by its ID.
    /// </summary>
    /// <param name="id">The comment ID.</param>
    /// <returns>The comment profile.</returns>
    /// <response code="200">Comment found.</response>
    /// <response code="404">Comment not found.</response>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetComment(int id)
    {
        var comment = await _commentService.GetCommentByIdAsync(id);
        if (comment is null)
            return NotFound(new { message = "Comment not found." });

        return Ok(comment);
    }

    // GET /api/comments/post/{id}
    /// <summary>
    /// Retrieves all comments for a specific post.
    /// </summary>
    /// <param name="id">The post ID.</param>
    /// <returns>A list of comments.</returns>
    [HttpGet("post/{id:int}")]
    public async Task<IActionResult> GetByPost(int id)
    {
        var comments = await _commentService.GetByPostAsync(id);
        return Ok(comments);
    }

    // GET /api/comments/toplevel/{postId}
    /// <summary>
    /// Retrieves top-level comments (not replies) for a post.
    /// </summary>
    /// <param name="postId">The post ID.</param>
    /// <returns>A list of top-level comments.</returns>
    [HttpGet("toplevel/{postId:int}")]
    public async Task<IActionResult> GetTopLevel(int postId)
    {
        var comments = await _commentService.GetTopLevelAsync(postId);
        return Ok(comments);
    }

    // GET /api/comments/replies/{commentId}
    /// <summary>
    /// Retrieves all replies to a specific comment.
    /// </summary>
    /// <param name="commentId">The parent comment ID.</param>
    /// <returns>A list of reply comments.</returns>
    [HttpGet("replies/{commentId:int}")]
    public async Task<IActionResult> GetReplies(int commentId)
    {
        var replies = await _commentService.GetRepliesAsync(commentId);
        return Ok(replies);
    }

    // PUT /api/comments/{id}
    /// <summary>
    /// Edits an existing comment.
    /// </summary>
    /// <param name="id">The comment ID.</param>
    /// <param name="request">The updated comment content.</param>
    /// <returns>The updated comment.</returns>
    /// <response code="200">Comment updated.</response>
    /// <response code="403">If the user is not the author of the comment.</response>
    /// <response code="404">Comment not found.</response>
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
    /// <summary>
    /// Deletes a comment.
    /// </summary>
    /// <param name="id">The comment ID.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Comment deleted.</response>
    /// <response code="403">If the user is not the author.</response>
    /// <response code="404">Comment not found.</response>
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
    /// <summary>
    /// Gets the total number of comments for a post.
    /// </summary>
    /// <param name="postId">The post ID.</param>
    /// <returns>The comment count.</returns>
    [HttpGet("count/{postId:int}")]
    public async Task<IActionResult> GetCount(int postId)
    {
        var count = await _commentService.GetCommentCountAsync(postId);
        return Ok(new { count });
    }

    // DELETE /api/comments/admin/{id}
    /// <summary>
    /// Deletes a comment as an administrator.
    /// </summary>
    /// <param name="id">The comment ID.</param>
    /// <returns>No content.</returns>
    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/{id:int}")]
    public async Task<IActionResult> AdminDelete(int id)
    {
        await _commentService.AdminDeleteCommentAsync(id);
        return NoContent();
    }
}
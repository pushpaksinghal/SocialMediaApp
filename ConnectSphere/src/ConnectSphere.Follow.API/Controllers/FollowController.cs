using System.Security.Claims;
using ConnectSphere.Follow.API.DTOs;
using ConnectSphere.Follow.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectSphere.Follow.API.Controllers;

[ApiController]
[Route("api/follows")]
public class FollowController : ControllerBase
{
    private readonly IFollowService _followService;

    public FollowController(IFollowService followService)
    {
        _followService = followService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetAccessToken() =>
        HttpContext.Request.Headers.Authorization
            .ToString()["Bearer ".Length..].Trim();

    // POST /api/follows
    /// <summary>
    /// Follows a user or sends a follow request.
    /// </summary>
    /// <param name="request">The follow request containing FolloweeId.</param>
    /// <returns>The created follow relationship or request.</returns>
    /// <response code="201">Follow relationship or request created.</response>
    /// <response code="409">Conflict (e.g., already following or pending).</response>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Follow([FromBody] FollowRequest request)
    {
        try
        {
            var follow = await _followService.FollowUserAsync(
                GetUserId(), request, GetAccessToken());
            return StatusCode(201, follow);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT /api/follows/{id}/accept
    /// <summary>
    /// Accepts a pending follow request.
    /// </summary>
    /// <param name="id">The follow request ID.</param>
    /// <returns>The updated follow relationship.</returns>
    /// <response code="200">Request accepted.</response>
    /// <response code="400">Invalid request state.</response>
    /// <response code="404">Request not found.</response>
    [Authorize]
    [HttpPut("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
    {
        try
        {
            var follow = await _followService.AcceptFollowRequestAsync(
                id, GetUserId(), GetAccessToken());
            return Ok(follow);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE /api/follows/{followeeId}
    /// <summary>
    /// Unfollows a user.
    /// </summary>
    /// <param name="followeeId">The ID of the user to unfollow.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Unfollowed successfully.</response>
    /// <response code="404">Relationship not found.</response>
    [Authorize]
    [HttpDelete("{followeeId:int}")]
    public async Task<IActionResult> Unfollow(int followeeId)
    {
        try
        {
            await _followService.UnfollowUserAsync(
                GetUserId(), followeeId, GetAccessToken());
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    // PUT /api/follows/{id}/reject
    /// <summary>
    /// Rejects a pending follow request.
    /// </summary>
    /// <param name="id">The follow request ID.</param>
    /// <returns>The updated follow relationship (rejected state).</returns>
    /// <response code="200">Request rejected.</response>
    /// <response code="404">Request not found.</response>
    [Authorize]
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        try
        {
            var follow = await _followService.RejectFollowRequestAsync(
                id, GetUserId());
            return Ok(follow);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET /api/follows/{userId}/followers
    /// <summary>
    /// Retrieves the list of followers for a specific user.
    /// </summary>
    /// <param name="userId">The User ID.</param>
    /// <returns>A list of followers.</returns>
    [Authorize]
    [HttpGet("{userId:int}/followers")]
    public async Task<IActionResult> GetFollowers(int userId)
    {
        var followers = await _followService.GetFollowersAsync(userId);
        return Ok(followers);
    }

    // GET /api/follows/{userId}/following
    /// <summary>
    /// Retrieves the list of users a specific user is following.
    /// </summary>
    /// <param name="userId">The User ID.</param>
    /// <returns>A list of users being followed.</returns>
    [Authorize]
    [HttpGet("{userId:int}/following")]
    public async Task<IActionResult> GetFollowing(int userId)
    {
        var following = await _followService.GetFollowingAsync(userId);
        return Ok(following);
    }

    // GET /api/follows/pending
    /// <summary>
    /// Retrieves all pending follow requests for the current user.
    /// </summary>
    /// <returns>A list of pending follow requests.</returns>
    [Authorize]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var pending = await _followService.GetPendingRequestsAsync(GetUserId());
        return Ok(pending);
    }

    // GET /api/follows/is/{followeeId}
    /// <summary>
    /// Checks if the current user is following another user.
    /// </summary>
    /// <param name="followeeId">The ID of the user to check.</param>
    /// <returns>A boolean status.</returns>
    [Authorize]
    [HttpGet("is/{followeeId:int}")]
    public async Task<IActionResult> IsFollowing(int followeeId)
    {
        var result = await _followService.IsFollowingAsync(
            GetUserId(), followeeId);
        return Ok(new { isFollowing = result });
    }


    // GET /api/follows/ids/{userId}
    /// <summary>
    /// Retrieves the IDs of all users a specific user is following.
    /// </summary>
    /// <param name="userId">The User ID.</param>
    /// <returns>A list of user IDs.</returns>
    [Authorize]
    [HttpGet("ids/{userId:int}")]
    public async Task<IActionResult> GetFollowingIds(int userId)
    {
        var ids = await _followService.GetFollowingIdsAsync(userId);
        return Ok(ids);
    }

    // GET /api/follows/mutual/{userIdA}/{userIdB}
    /// <summary>
    /// Retrieves mutual followers between two users.
    /// </summary>
    /// <param name="userIdA">First user ID.</param>
    /// <param name="userIdB">Second user ID.</param>
    /// <returns>A list of mutual followers.</returns>
    [Authorize]
    [HttpGet("mutual/{userIdA:int}/{userIdB:int}")]
    public async Task<IActionResult> GetMutual(int userIdA, int userIdB)
    {
        var mutual = await _followService.GetMutualFollowersAsync(
            userIdA, userIdB);
        return Ok(mutual);
    }
}
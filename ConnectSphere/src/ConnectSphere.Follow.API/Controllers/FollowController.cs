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

    // POST /api/follows
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Follow([FromBody] FollowRequest request)
    {
        try
        {
            var follow = await _followService.FollowUserAsync(
                GetUserId(), request);
            return StatusCode(201, follow);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT /api/follows/{id}/accept
    [Authorize]
    [HttpPut("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
    {
        try
        {
            var follow = await _followService.AcceptFollowRequestAsync(
                id, GetUserId());
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

    // PUT /api/follows/{id}/reject
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

    // DELETE /api/follows/{followeeId}
    [Authorize]
    [HttpDelete("{followeeId:int}")]
    public async Task<IActionResult> Unfollow(int followeeId)
    {
        try
        {
            await _followService.UnfollowUserAsync(GetUserId(), followeeId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET /api/follows/{userId}/followers
    [Authorize]
    [HttpGet("{userId:int}/followers")]
    public async Task<IActionResult> GetFollowers(int userId)
    {
        var followers = await _followService.GetFollowersAsync(userId);
        return Ok(followers);
    }

    // GET /api/follows/{userId}/following
    [Authorize]
    [HttpGet("{userId:int}/following")]
    public async Task<IActionResult> GetFollowing(int userId)
    {
        var following = await _followService.GetFollowingAsync(userId);
        return Ok(following);
    }

    // GET /api/follows/pending
    [Authorize]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var pending = await _followService.GetPendingRequestsAsync(GetUserId());
        return Ok(pending);
    }

    // GET /api/follows/is/{followeeId}
    [Authorize]
    [HttpGet("is/{followeeId:int}")]
    public async Task<IActionResult> IsFollowing(int followeeId)
    {
        var result = await _followService.IsFollowingAsync(
            GetUserId(), followeeId);
        return Ok(new { isFollowing = result });
    }

    // GET /api/follows/ids/{userId}
    [Authorize]
    [HttpGet("ids/{userId:int}")]
    public async Task<IActionResult> GetFollowingIds(int userId)
    {
        var ids = await _followService.GetFollowingIdsAsync(userId);
        return Ok(ids);
    }

    // GET /api/follows/mutual/{userIdA}/{userIdB}
    [Authorize]
    [HttpGet("mutual/{userIdA:int}/{userIdB:int}")]
    public async Task<IActionResult> GetMutual(int userIdA, int userIdB)
    {
        var mutual = await _followService.GetMutualFollowersAsync(
            userIdA, userIdB);
        return Ok(mutual);
    }
}
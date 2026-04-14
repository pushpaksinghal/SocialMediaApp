using System.Security.Claims;
using ConnectSphere.Notif.API.DTOs;
using ConnectSphere.Notif.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectSphere.Notif.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotifController : ControllerBase
{
    private readonly INotifService _notifService;

    public NotifController(INotifService notifService)
    {
        _notifService = notifService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/notifications/{userId}?page=1&pageSize=20
    [Authorize]
    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetNotifications(
        int userId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
    {
        var notifications = await _notifService
            .GetNotificationsAsync(userId, page, pageSize);
        return Ok(notifications);
    }

    // GET /api/notifications/unread
    [Authorize]
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread()
    {
        var notifications = await _notifService.GetUnreadAsync(GetUserId());
        return Ok(notifications);
    }

    // GET /api/notifications/unread/count
    [Authorize]
    [HttpGet("unread/count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notifService.GetUnreadCountAsync(GetUserId());
        return Ok(new { count });
    }

    // PUT /api/notifications/{id}/read
    [Authorize]
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notifService.MarkAsReadAsync(id, GetUserId());
        return Ok(new { message = "Marked as read." });
    }

    // PUT /api/notifications/read-all
    [Authorize]
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifService.MarkAllReadAsync(GetUserId());
        return Ok(new { message = "All notifications marked as read." });
    }

    // DELETE /api/notifications/{id}
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _notifService.DeleteNotificationAsync(id, GetUserId());
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // POST /api/notifications/send (internal use — called by other services)
    [Authorize]
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendNotifRequest request)
    {
        var notif = await _notifService.SendAsync(request);
        return StatusCode(201, notif);
    }

    // POST /api/notifications/broadcast (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast(
        [FromBody] BroadcastRequest request)
    {
        await _notifService.SendBulkAsync(request);
        return Ok(new { message = $"Broadcast sent to {request.UserIds.Count} users." });
    }
}
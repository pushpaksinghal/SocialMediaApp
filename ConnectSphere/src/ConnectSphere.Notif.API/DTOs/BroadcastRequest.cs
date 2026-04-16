using System.ComponentModel.DataAnnotations;

namespace ConnectSphere.Notif.API.DTOs;

public class BroadcastRequest
{
    [Required]
    public List<int> UserIds { get; set; } = new();

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;
}
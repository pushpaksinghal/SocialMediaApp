using System.ComponentModel.DataAnnotations;

namespace ConnectSphere.Notif.API.DTOs;

public class SendNotifRequest
{
    [Required]
    public int RecipientId { get; set; }

    [Required]
    public int ActorId { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public int? TargetId { get; set; }

    public string? TargetType { get; set; }
}
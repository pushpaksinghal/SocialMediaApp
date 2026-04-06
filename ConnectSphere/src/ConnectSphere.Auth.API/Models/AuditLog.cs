using System.ComponentModel.DataAnnotations;

namespace ConnectSphere.Auth.API.Models;

public class AuditLog
{
    public int AuditLogId { get; set; }

    public int ActorId { get; set; }

    [MaxLength(100)]
    public string ActionType { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
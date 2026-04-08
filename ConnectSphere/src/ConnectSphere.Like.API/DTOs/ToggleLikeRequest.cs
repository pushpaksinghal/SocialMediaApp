using System.ComponentModel.DataAnnotations;

namespace ConnectSphere.Like.API.DTOs;

public class ToggleLikeRequest
{
    [Required]
    public int TargetId { get; set; }

    [Required]
    public string TargetType { get; set; } = string.Empty; // POST or COMMENT
}
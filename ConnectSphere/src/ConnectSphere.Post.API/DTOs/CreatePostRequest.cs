using System.ComponentModel.DataAnnotations;

namespace ConnectSphere.Post.API.DTOs;

public class CreatePostRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;
    [MaxLength(12)]
    public string Visibility { get; set; } = "PUBLIC";
    public string? Hashtags { get; set; }
    public string? MediaUrl { get; set; }
}
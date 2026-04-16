using System.ComponentModel.DataAnnotations;

namespace ConnectSphere.Comment.API.DTOs;

public class AddCommentRequest
{
    [Required]
    public int PostId { get; set; }

    public int? ParentCommentId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;
}
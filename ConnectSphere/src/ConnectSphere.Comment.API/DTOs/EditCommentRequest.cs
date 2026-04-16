using System.ComponentModel.DataAnnotations;

namespace ConnectSphere.Comment.API.DTOs;

public class EditCommentRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;
}
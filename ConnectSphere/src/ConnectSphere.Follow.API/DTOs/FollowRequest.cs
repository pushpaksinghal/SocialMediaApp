using System.ComponentModel.DataAnnotations;

namespace ConnectSphere.Follow.API.DTOs;

public class FollowRequest
{
    [Required]
    public int FolloweeId { get; set; }

    // true = private account, false = public account
    public bool IsPrivate { get; set; } = false;
}
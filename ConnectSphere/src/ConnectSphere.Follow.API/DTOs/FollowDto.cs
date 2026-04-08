namespace ConnectSphere.Follow.API.DTOs;

public class FollowDto
{
    public int FollowId { get; set; }
    public int FollowerId { get; set; }
    public int FolloweeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
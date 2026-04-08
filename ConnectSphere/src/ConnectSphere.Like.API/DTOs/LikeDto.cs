namespace ConnectSphere.Like.API.DTOs;

public class LikeDto
{
    public int LikeId { get; set; }
    public int UserId { get; set; }
    public int TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
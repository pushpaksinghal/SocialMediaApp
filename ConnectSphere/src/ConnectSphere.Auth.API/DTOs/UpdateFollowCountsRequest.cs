namespace ConnectSphere.Auth.API.DTOs;

public class UpdateFollowCountsRequest
{
    public int FollowerId { get; set; }
    public int FolloweeId { get; set; }
    public bool Increment { get; set; }
}
using ConnectSphere.Follow.API.DTOs;

namespace ConnectSphere.Follow.API.Services;

public interface IFollowService
{
    Task<FollowDto> FollowUserAsync(int followerId, FollowRequest request);
    Task<FollowDto> AcceptFollowRequestAsync(int followId, int userId);
    Task<FollowDto> RejectFollowRequestAsync(int followId, int userId);
    Task UnfollowUserAsync(int followerId, int followeeId);
    Task<List<FollowDto>> GetFollowersAsync(int userId);
    Task<List<FollowDto>> GetFollowingAsync(int userId);
    Task<List<FollowDto>> GetPendingRequestsAsync(int userId);
    Task<bool> IsFollowingAsync(int followerId, int followeeId);
    Task<List<int>> GetFollowingIdsAsync(int userId);
    Task<List<int>> GetMutualFollowersAsync(int userIdA, int userIdB);
}
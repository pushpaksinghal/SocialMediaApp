using ConnectSphere.Like.API.DTOs;

namespace ConnectSphere.Like.API.Services;

public interface ILikeService
{
    Task<ToggleLikeResponse> ToggleLikeAsync(int userId, ToggleLikeRequest request);
    Task<List<LikeDto>> GetLikesByTargetAsync(int targetId, string targetType);
    Task<List<LikeDto>> GetLikesByUserAsync(int userId);
    Task<int> GetLikeCountAsync(int targetId, string targetType);
    Task<bool> HasUserLikedAsync(int userId, int targetId, string targetType);
    Task<List<int>> GetLikersForPostAsync(int postId);
    Task<List<int>> GetLikedPostsByUserAsync(int userId);
}
using ConnectSphere.Post.API.DTOs;

namespace ConnectSphere.Post.API.Services;

public interface IPostService
{
    Task<PostDto> CreatePostAsync(int userId, CreatePostRequest request, IFormFile? media,string accessToken);
    Task<PostDto?> GetPostByIdAsync(int postId, int? requestingUserId);
    Task<List<PostDto>> GetPostsByUserAsync(int userId, int? requestingUserId);
    Task<PostDto> UpdatePostAsync(int postId, int userId, UpdatePostRequest request);
    Task SoftDeletePostAsync(int postId, int userId,string accessToken);
    Task<List<PostDto>> GetByHashtagAsync(string tag);
    Task<List<PostDto>> SearchPostsAsync(string query);
    Task<List<PostDto>> GetTrendingPostsAsync();
    Task<PostDto> SharePostAsync(int originalPostId, int userId);
    Task<List<PostDto>> GetPublicFeedAsync();
    Task IncrementCommentCountAsync(int postId);
    Task SyncLikeCountAsync(int postId, int count);
    Task AdminDeletePostAsync(int postId);
}
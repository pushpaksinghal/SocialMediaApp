using ConnectSphere.Comment.API.DTOs;

namespace ConnectSphere.Comment.API.Services;

public interface ICommentService
{
    Task<CommentDto> AddCommentAsync(int userId, AddCommentRequest request,string accessToken);
    Task<CommentDto?> GetCommentByIdAsync(int commentId);
    Task<List<CommentDto>> GetByPostAsync(int postId);
    Task<List<CommentDto>> GetTopLevelAsync(int postId);
    Task<List<CommentDto>> GetRepliesAsync(int commentId);
    Task<CommentDto> EditCommentAsync(int commentId, int userId, EditCommentRequest request);
    Task DeleteCommentAsync(int commentId, int userId);
    Task<int> GetCommentCountAsync(int postId);
    Task AdminDeleteCommentAsync(int commentId);
}
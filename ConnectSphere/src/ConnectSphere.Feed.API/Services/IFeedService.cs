using ConnectSphere.Feed.API.DTOs;

namespace ConnectSphere.Feed.API.Services;

public interface IFeedService
{
    Task<FeedResponseDto> GetHomeFeedAsync(
        int userId, string accessToken, int page, int pageSize);
    Task<List<FeedPostDto>> GetExploreFeedAsync(
        int userId, string accessToken);
    Task<List<FeedPostDto>> GetUserTimelineAsync(int userId);
    Task<List<TrendingHashtagDto>> GetTrendingHashtagsAsync(int topN);
    Task InvalidateFeedCacheAsync(int userId);
}
namespace ConnectSphere.Feed.API.DTOs;

public class FeedResponseDto
{
    public List<FeedPostDto> Posts { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool FromCache { get; set; }
}
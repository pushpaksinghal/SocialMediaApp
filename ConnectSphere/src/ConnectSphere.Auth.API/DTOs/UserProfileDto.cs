namespace ConnectSphere.Auth.API.DTOs;

/// <summary>
/// Data transfer object representing a user's profile information.
/// </summary>
public class UserProfileDto
{
    /// <summary>The unique identifier for the user.</summary>
    public int UserId { get; set; }
    /// <summary>The user's unique handle/username.</summary>
    public string UserName { get; set; } = string.Empty;
    /// <summary>The user's display name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>The user's email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>A short biography or description of the user.</summary>
    public string? Bio { get; set; }
    /// <summary>URL to the user's profile picture.</summary>
    public string? AvatarUrl { get; set; }
    /// <summary>Whether the account is private (requires approval for followers).</summary>
    public bool IsPrivate { get; set; }
    /// <summary>Number of users following this user.</summary>
    public int FollowerCount { get; set; }
    /// <summary>Number of users this user is following.</summary>
    public int FollowingCount { get; set; }
    /// <summary>Number of posts created by this user.</summary>
    public int PostCount { get; set; }
    /// <summary>The timestamp when the profile was created.</summary>
    public DateTime CreatedAt { get; set; }
}
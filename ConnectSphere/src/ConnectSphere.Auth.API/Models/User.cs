using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Auth.API.Models;

[Index(nameof(UserName),IsUnique =true)]
[Index(nameof(Email), IsUnique =true)]
public class User
{
    public int UserId { get; set; }

    [MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(512)]
    public string PasswordHash { get; set; } = string.Empty;

    public string? Bio { get; set; }

    [MaxLength(512)]
    public string? AvatarUrl { get; set; }

    public bool IsPrivate { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public int FollowerCount { get; set; } = 0;

    public int FollowingCount { get; set; } = 0;

    public int PostCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
using System.ComponentModel.DataAnnotations;

namespace ConnectSphere.Auth.API.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
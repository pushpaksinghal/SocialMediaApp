using System.ComponentModel.DataAnnotations;

namespace ConnectSphere.Auth.API.DTOs;

public class UpdateProfileRequest
{
    [MaxLength(120)]
    public string? FullName { get; set; }
    public string? Bio { get; set; }
}
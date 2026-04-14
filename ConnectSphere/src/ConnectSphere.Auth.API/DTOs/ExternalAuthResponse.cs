namespace ConnectSphere.Auth.API.DTOs;

public class ExternalAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserProfileDto User { get; set; } = null!;
    public bool IsNewUser { get; set; }
}
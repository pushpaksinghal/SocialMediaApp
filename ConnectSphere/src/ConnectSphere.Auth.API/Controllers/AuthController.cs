using System.Security.Claims;
using ConnectSphere.Auth.API.DTOs;
using ConnectSphere.Auth.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectSphere.Auth.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return StatusCode(201, response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // POST /api/auth/logout
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var accessToken = HttpContext.Request.Headers.Authorization
            .ToString()["Bearer ".Length..].Trim();

        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await _authService.LogoutAsync(userId, accessToken, request.RefreshToken);
        return Ok(new { message = "Logged out successfully." });
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // PUT /api/auth/profile/{id}
    [Authorize]
    [HttpPut("profile/{id:int}")]
    public async Task<IActionResult> UpdateProfile(
        int id,
        [FromForm] UpdateProfileRequest request,
        IFormFile? avatar)
    {
        var callerId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (callerId != id)
            return Forbid();

        try
        {
            var profile = await _authService.UpdateProfileAsync(id, request, avatar);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // PUT /api/auth/password/{id}
    [Authorize]
    [HttpPut("password/{id:int}")]
    public async Task<IActionResult> ChangePassword(
        int id, [FromBody] ChangePasswordRequest request)
    {
        var callerId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (callerId != id)
            return Forbid();

        try
        {
            await _authService.ChangePasswordAsync(id, request);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // PATCH /api/auth/privacy/{id}
    [Authorize]
    [HttpPatch("privacy/{id:int}")]
    public async Task<IActionResult> TogglePrivacy(int id)
    {
        var callerId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (callerId != id)
            return Forbid();

        await _authService.TogglePrivacyAsync(id);
        return Ok(new { message = "Privacy setting updated." });
    }

    // GET /api/auth/{userName}
    [HttpGet("{userName}")]
    public async Task<IActionResult> GetProfile(string userName)
    {
        var profile = await _authService.GetProfileAsync(userName);
        if (profile is null)
            return NotFound(new { message = "User not found." });

        return Ok(profile);
    }

    // GET /api/auth/search?q=
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query cannot be empty." });

        var results = await _authService.SearchUsersAsync(q);
        return Ok(results);
    }

    // GET /api/auth/suggestions
    [Authorize]
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var suggestions = await _authService.GetSuggestionsAsync(userId);
        return Ok(suggestions);
    }

    // DELETE /api/auth/deactivate
    [Authorize]
    [HttpDelete("deactivate")]
    public async Task<IActionResult> Deactivate()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await _authService.DeactivateAccountAsync(userId);
        return Ok(new { message = "Account deactivated." });
    }
    // GET /api/auth/login/google
    [HttpGet("login/google")]
    public IActionResult LoginWithGoogle(
        [FromQuery] string returnUrl = "/api/auth/callback/google")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), "Auth"),
            Items =
            {
                { "LoginProvider", "Google" }
            }
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    // GET /api/auth/callback/google
    [HttpGet("callback/google")]
    public async Task<IActionResult> GoogleCallback()
    {
        // Read the cookie that Google signed us into
        var result = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal == null)
            return Unauthorized(new { message = "Google login failed." });

        var claims = result.Principal.Claims.ToList();

        var email = claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Email)?.Value ?? string.Empty;

        var fullName = claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Name)?.Value ?? string.Empty;

        var userName = claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.GivenName)?.Value
            ?? email.Split('@')[0];

        if (string.IsNullOrEmpty(email))
            return BadRequest(new { message = "Could not retrieve email from Google." });

        try
        {
            var response = await _authService.ExternalLoginAsync(
                email, fullName, userName, "Google");

            // Sign out of cookie session — we use JWT from here
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // GET /api/auth/login/github
    [HttpGet("login/github")]
    public IActionResult LoginWithGitHub()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GitHubCallback), "Auth"),
            Items =
            {
                { "LoginProvider", "GitHub" }
            }
        };

        return Challenge(properties, "GitHub");
    }

    // GET /api/auth/callback/github
    [HttpGet("callback/github")]
    public async Task<IActionResult> GitHubCallback()
    {
        var result = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal == null)
            return Unauthorized(new { message = "GitHub login failed." });

        var claims = result.Principal.Claims.ToList();

        var email = claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Email)?.Value ?? string.Empty;

        var fullName = claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Name)?.Value ?? string.Empty;

        var userName = claims.FirstOrDefault(c =>
            c.Type == "urn:github:login")?.Value
            ?? email.Split('@')[0];

        if (string.IsNullOrEmpty(email))
            return BadRequest(new
            {
                message = "GitHub account must have a public email."
            });

        try
        {
            var response = await _authService.ExternalLoginAsync(
                email, fullName, userName, "GitHub");

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
    // POST /api/auth/update-counts
    [Authorize]
    [HttpPost("update-counts")]
    public async Task<IActionResult> UpdateFollowCounts(
        [FromBody] UpdateFollowCountsRequest request)
    {
        await _authService.UpdateFollowCountsAsync(
            request.FollowerId,
            request.FolloweeId,
            request.Increment);

        return Ok();
    }
    // POST /api/auth/increment-post/{userId}
    [Authorize]
    [HttpPost("increment-post/{userId:int}")]
    public async Task<IActionResult> IncrementPostCount(int userId)
    {
        await _authService.UpdatePostCountAsync(userId, increment: true);
        return Ok();
    }

    // POST /api/auth/decrement-post/{userId}
    [Authorize]
    [HttpPost("decrement-post/{userId:int}")]
    public async Task<IActionResult> DecrementPostCount(int userId)
    {
        await _authService.UpdatePostCountAsync(userId, increment: false);
        return Ok();
    }
}
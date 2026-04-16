using System.Security.Claims;
using ConnectSphere.Auth.API.DTOs;
using ConnectSphere.Auth.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ConnectSphere.Auth.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;

    public AuthController(IAuthService authService, IConfiguration config)
    {
        _authService = authService;
        _config = config;
    }

    // POST /api/auth/register
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">The registration details.</param>
    /// <returns>The created user profile and tokens.</returns>
    /// <response code="201">Returns the newly created user.</response>
    /// <response code="409">If the email or username already exists.</response>
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
    /// <summary>
    /// Authenticates a user and returns tokens.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>User profile and access/refresh tokens.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials.</response>
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
    /// <summary>
    /// Logs out the user by blacklisting the current tokens.
    /// </summary>
    /// <param name="request">Request containing the refresh token to invalidate.</param>
    /// <returns>A success message.</returns>
    /// <response code="200">Logout successful.</response>
    /// <response code="401">Unauthorized.</response>
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
    /// <summary>
    /// Refreshes the access token using a valid refresh token.
    /// </summary>
    /// <param name="request">Request containing the refresh token.</param>
    /// <returns>A new set of tokens.</returns>
    /// <response code="200">Refresh successful.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
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
    /// <summary>
    /// Updates the user's profile information and avatar.
    /// </summary>
    /// <param name="id">The User ID.</param>
    /// <param name="request">The update details.</param>
    /// <param name="avatar">An optional avatar image file.</param>
    /// <returns>The updated profile.</returns>
    /// <response code="200">Profile updated successfully.</response>
    /// <response code="403">If attempting to update someone else's profile.</response>
    /// <response code="404">User not found.</response>
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
    /// <summary>
    /// Changes the user's password.
    /// </summary>
    /// <param name="id">The User ID.</param>
    /// <param name="request">Old and new password details.</param>
    /// <returns>A success message.</returns>
    /// <response code="200">Password changed successfully.</response>
    /// <response code="401">Incorrect old password.</response>
    /// <response code="403">Forbidden.</response>
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
    /// <summary>
    /// Toggles the account privacy between public and private.
    /// </summary>
    /// <param name="id">The User ID.</param>
    /// <returns>A success message.</returns>
    /// <response code="200">Privacy setting updated.</response>
    /// <response code="403">Forbidden.</response>
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
    /// <summary>
    /// Retrieves a user's profile by their username.
    /// </summary>
    /// <param name="userName">The unique username.</param>
    /// <returns>The user profile.</returns>
    /// <response code="200">User found.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{userName}")]
    public async Task<IActionResult> GetProfile(string userName)
    {
        var profile = await _authService.GetProfileAsync(userName);
        if (profile is null)
            return NotFound(new { message = "User not found." });

        return Ok(profile);
    }

    // GET /api/auth/search?q=
    /// <summary>
    /// Searches for users by name or username.
    /// </summary>
    /// <param name="q">The search query string.</param>
    /// <returns>A list of matching user profiles.</returns>
    /// <response code="200">Search results (can be empty).</response>
    /// <response code="400">Empty query.</response>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query cannot be empty." });

        var results = await _authService.SearchUsersAsync(q);
        return Ok(results);
    }

    // GET /api/auth/suggestions
    /// <summary>
    /// Gets suggested users to follow for the current user.
    /// </summary>
    /// <returns>A list of suggested profiles.</returns>
    /// <response code="200">List of suggestions.</response>
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
    /// <summary>
    /// Deactivates the current user's account.
    /// </summary>
    /// <returns>A success message.</returns>
    /// <response code="200">Account deactivated.</response>
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
    /// <summary>
    /// Initiates a Google OAuth login challenge.
    /// </summary>
    /// <param name="returnUrl">The URL to return to after authentication.</param>
    /// <returns>A challenge result triggering Google login.</returns>
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
    /// <summary>
    /// Callback endpoint for Google OAuth authentication.
    /// </summary>
    /// <returns>A redirect to the frontend with tokens.</returns>
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

        var avatarUrl = claims.FirstOrDefault(c => c.Type == "picture")?.Value 
            ?? claims.FirstOrDefault(c => c.Type == "image")?.Value;

        if (string.IsNullOrEmpty(email))
            return BadRequest(new { message = "Could not retrieve email from Google." });

        try
        {
            var response = await _authService.ExternalLoginAsync(
                email, fullName, userName, "Google", avatarUrl);

            // Sign out of cookie session — we use JWT from here
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            var frontendUrl = _config["Cors:AllowedOrigin"] ?? "http://localhost:4200";
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var userJson = JsonSerializer.Serialize(response.User, options);
            var redirectUrl = $"{frontendUrl}/auth/callback?" +
                              $"accessToken={response.AccessToken}&" +
                              $"refreshToken={response.RefreshToken}&" +
                              $"user={Uri.EscapeDataString(userJson)}";

            return Redirect(redirectUrl);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // GET /api/auth/login/github
    /// <summary>
    /// Initiates a GitHub OAuth login challenge.
    /// </summary>
    /// <returns>A challenge result triggering GitHub login.</returns>
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
    /// <summary>
    /// Callback endpoint for GitHub OAuth authentication.
    /// </summary>
    /// <returns>A redirect to the frontend with tokens.</returns>
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
            c.Type == ClaimTypes.Name)?.Value 
            ?? claims.FirstOrDefault(c => c.Type == "name")?.Value
            ?? string.Empty;
 
        var userName = claims.FirstOrDefault(c =>
            c.Type == "urn:github:login")?.Value
            ?? claims.FirstOrDefault(c => c.Type == "login")?.Value
            ?? email.Split('@')[0];

        var avatarUrl = claims.FirstOrDefault(c => c.Type == "urn:github:avatar")?.Value 
            ?? claims.FirstOrDefault(c => c.Type == "avatar_url")?.Value;

        if (string.IsNullOrEmpty(email))
            return BadRequest(new
            {
                message = "GitHub account must have a public email."
            });

        try
        {
            var response = await _authService.ExternalLoginAsync(
                email, fullName, userName, "GitHub", avatarUrl);

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            var frontendUrl = _config["Cors:AllowedOrigin"] ?? "http://localhost:4200";
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var userJson = JsonSerializer.Serialize(response.User, options);
            var redirectUrl = $"{frontendUrl}/auth/callback?" +
                              $"accessToken={response.AccessToken}&" +
                              $"refreshToken={response.RefreshToken}&" +
                              $"user={Uri.EscapeDataString(userJson)}";

            return Redirect(redirectUrl);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
    // POST /api/auth/update-counts
    /// <summary>
    /// Updates follower/following counts for users. (Internal use)
    /// </summary>
    /// <param name="request">The update request containing IDs and increment flag.</param>
    /// <returns>An Ok result.</returns>
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
    /// <summary>
    /// Increments the post count for a user. (Internal use)
    /// </summary>
    /// <param name="userId">The User ID.</param>
    /// <returns>An Ok result.</returns>
    [Authorize]
    [HttpPost("increment-post/{userId:int}")]
    public async Task<IActionResult> IncrementPostCount(int userId)
    {
        await _authService.UpdatePostCountAsync(userId, increment: true);
        return Ok();
    }

    // POST /api/auth/decrement-post/{userId}
    /// <summary>
    /// Decrements the post count for a user. (Internal use)
    /// </summary>
    /// <param name="userId">The User ID.</param>
    /// <returns>An Ok result.</returns>
    [Authorize]
    [HttpPost("decrement-post/{userId:int}")]
    public async Task<IActionResult> DecrementPostCount(int userId)
    {
        await _authService.UpdatePostCountAsync(userId, increment: false);
        return Ok();
    }

    // GET /api/auth/userid/{id}  — internal: lookup by numeric user ID
    /// <summary>
    /// Retrieves a user's profile by their numeric ID.
    /// </summary>
    /// <param name="id">The User ID.</param>
    /// <returns>The user profile.</returns>
    /// <response code="200">User found.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("userid/{id:int}")]
    public async Task<IActionResult> GetProfileById(int id)
    {
        var profile = await _authService.GetProfileByIdAsync(id);
        if (profile is null)
            return NotFound(new { message = "User not found." });

        return Ok(profile);
    }

    // POST /api/auth/users/batch  — internal: batch lookup by list of IDs
    /// <summary>
    /// Retrieves multiple user profiles by a list of IDs.
    /// </summary>
    /// <param name="ids">List of User IDs.</param>
    /// <returns>A list of user profiles.</returns>
    /// <response code="200">List of user profiles.</response>
    [HttpPost("users/batch")]
    public async Task<IActionResult> GetProfilesBatch([FromBody] List<int> ids)
    {
        if (ids == null || ids.Count == 0)
            return Ok(new List<object>());

        var profiles = await _authService.GetProfilesByIdsAsync(ids);
        return Ok(profiles);
    }
}
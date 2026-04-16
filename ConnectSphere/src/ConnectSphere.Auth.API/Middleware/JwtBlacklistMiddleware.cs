using ConnectSphere.Auth.API.Services;

namespace ConnectSphere.Auth.API.Middleware;

public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    // Skip blacklist check for these paths
    private static readonly string[] SkipPaths =
[
    "/signin-google",
    "/signin-github",
    "/api/auth/login/google",
    "/api/auth/login/github",
    "/api/auth/callback/google",
    "/api/auth/callback/github"
];

    public JwtBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip OAuth paths entirely
        if (SkipPaths.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();

        if (authHeader.StartsWith("Bearer "))
        {
            var token       = authHeader["Bearer ".Length..].Trim();
            var blacklisted = await tokenService
                .IsAccessTokenBlacklistedAsync(token);

            if (blacklisted)
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(
                    new { message = "Token has been revoked." });
                return;
            }
        }

        await _next(context);
    }
}
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Render PORT Configuration ─────────────────────────────────────────────────
// Render sets the PORT environment variable at runtime.
// UseUrls only — ConfigureKestrel duplicate removed (caused port override warnings)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}
// ─────────────────────────────────────────────────────────────────────────────

// ── YARP Gateway ─────────────────────────────────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((context, handler) =>
    {
        handler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
    });

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Swagger Aggregation ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── CORS (Centralized) ───────────────────────────────────────────────────────
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = new List<string> { "https://social-media-app-frontend-dun.vercel.app/" };
        if (!string.IsNullOrEmpty(allowedOrigin))
            origins.Add(allowedOrigin);

        policy.WithOrigins(origins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ── Swagger (Development only) ───────────────────────────────────────────────

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger-auth/v1/swagger.json", "Auth API");
        options.SwaggerEndpoint("/swagger-post/v1/swagger.json", "Post API");
        options.SwaggerEndpoint("/swagger-comment/v1/swagger.json", "Comment API");
        options.SwaggerEndpoint("/swagger-feed/v1/swagger.json", "Feed API");
        options.SwaggerEndpoint("/swagger-follow/v1/swagger.json", "Follow API");
        options.SwaggerEndpoint("/swagger-like/v1/swagger.json", "Like API");
        options.SwaggerEndpoint("/swagger-notif/v1/swagger.json", "Notif API");
        options.RoutePrefix = "swagger";
    });


// No UseHttpsRedirection — backends are plain HTTP
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// ── Health check endpoint (required by Render) ────────────────────────────────
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapReverseProxy();

app.Run();
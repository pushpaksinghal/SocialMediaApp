using System.Reflection;
using System.Text;
using ConnectSphere.Feed.API.Data;
using ConnectSphere.Feed.API.HttpClients;
using ConnectSphere.Feed.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ========== ADD THIS: RENDER PORT CONFIGURATION ==========
// Render sets the PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
    
    // Configure Kestrel for Render (only if PORT is set)
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(int.Parse(port));
    });
}
// ==========================================================

// ── Database ──────────────────────────────────────────────────────────────────
// UPDATED: Use environment variable for connection string (Render friendly)
var connectionString = builder.Configuration.GetConnectionString("Default") 
    ?? builder.Configuration.GetConnectionString("FeedDb");

builder.Services.AddDbContext<FeedDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── Redis ─────────────────────────────────────────────────────────────────────
// UPDATED: Better Redis configuration for Render/Upstash
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? Environment.GetEnvironmentVariable("Redis__ConnectionString");

if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "ConnectSphere_Feed_";
    });
    Console.WriteLine("Redis caching enabled");
}
else
{
    Console.WriteLine("Warning: Redis not configured, caching disabled");
    // Add in-memory cache as fallback
    builder.Services.AddDistributedMemoryCache();
}

// ── HTTP Clients ──────────────────────────────────────────────────────────────
// UPDATED: Use environment variables for service URLs
var followServiceUrl = builder.Configuration["Services:FollowService"] 
    ?? Environment.GetEnvironmentVariable("FollowApi__BaseUrl")
    ?? "https://connectsphere-follow-api.onrender.com";

builder.Services.AddHttpClient<FollowServiceClient>(client =>
{
    client.BaseAddress = new Uri(followServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var postServiceUrl = builder.Configuration["Services:PostService"] 
    ?? Environment.GetEnvironmentVariable("PostApi__BaseUrl")
    ?? "https://connectsphere-post-api.onrender.com";

builder.Services.AddHttpClient<PostServiceClient>(client =>
{
    client.BaseAddress = new Uri(postServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── JWT Auth ──────────────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? builder.Configuration["JWT__Key"];
if (string.IsNullOrEmpty(jwtSecret))
    throw new Exception("JWT Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew                = TimeSpan.Zero
        };
        
        // ADDED: Better error handling for Render
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"Challenge: {context.Error}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IFeedService, FeedService>();

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "ConnectSphere Feed API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] 
    ?? builder.Configuration["CORS__ALLOWEDORIGIN"]
    ?? "https://connectsphere-frontend.vercel.app";  // Default for production

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ADDED: Health check endpoint for Render
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // ADDED: Redirect HTTP to HTTPS in production
    app.UseHttpsRedirection();
}

// ADDED: Health check endpoint
app.MapHealthChecks("/health");

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        Console.WriteLine("Feed database migration completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Feed database migration failed: {ex.Message}");
        // Don't throw - let the app start anyway
    }
}

app.Run();
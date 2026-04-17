using System.Reflection;
using System.Text;
using ConnectSphere.Follow.API.Data;
using ConnectSphere.Follow.API.HttpClients;
using ConnectSphere.Follow.API.Services;
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
    ?? builder.Configuration.GetConnectionString("FollowDb");

builder.Services.AddDbContext<FollowDbContext>(options =>
    options.UseNpgsql(connectionString));

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
builder.Services.AddScoped<IFollowService, FollowService>();

// ── HTTP Clients ──────────────────────────────────────────────────────────────
// UPDATED: Use environment variables for service URLs
var notifServiceUrl = builder.Configuration["Services:NotifService"] 
    ?? Environment.GetEnvironmentVariable("NotifApi__BaseUrl")
    ?? "https://connectsphere-notif-api.onrender.com";

builder.Services.AddHttpClient<NotifServiceClient>(client =>
{
    client.BaseAddress = new Uri(notifServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var authServiceUrl = builder.Configuration["Services:AuthService"] 
    ?? Environment.GetEnvironmentVariable("AuthApi__BaseUrl")
    ?? "https://connectsphere-auth-api.onrender.com";

builder.Services.AddHttpClient<AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(authServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "ConnectSphere Follow API",
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

    app.UseSwagger();
    app.UseSwaggerUI();


// ADDED: Health check endpoint
app.MapHealthChecks("/health");

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FollowDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        Console.WriteLine("Follow database migration completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Follow database migration failed: {ex.Message}");
        // Don't throw - let the app start anyway
    }
}

app.Run();
using System.Text;
using ConnectSphere.Auth.API.Data;
using ConnectSphere.Auth.API.Middleware;
using ConnectSphere.Auth.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")));

// ── Redis ─────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")!));

// ── JWT Auth ──────────────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
// ── Authentication ────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme       = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.SameSite  = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // http for local dev
})
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
})
.AddGoogle(options =>
{
    options.ClientId          = builder.Configuration["Google:ClientId"]!;
    options.ClientSecret      = builder.Configuration["Google:ClientSecret"]!;
    options.CallbackPath      = "/signin-google"; // ← use default path
    options.SignInScheme      = CookieAuthenticationDefaults.AuthenticationScheme;
    options.SaveTokens        = true;
})
.AddGitHub(options =>
{
    options.ClientId          = builder.Configuration["GitHub:ClientId"]!;
    options.ClientSecret      = builder.Configuration["GitHub:ClientSecret"]!;
    options.CallbackPath      = "/signin-github"; // ← use default path
    options.SignInScheme      = CookieAuthenticationDefaults.AuthenticationScheme;
    options.Scope.Add("user:email");
    options.SaveTokens        = true;
});
builder.Services.AddAuthorization();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();


// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "ConnectSphere Auth API",
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
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:AllowedOrigin"]!)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseMiddleware<JwtBlacklistMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
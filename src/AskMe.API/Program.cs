using System.Text;
using AskMe.API.Middleware;
using AskMe.Application.Admin;
using AskMe.Application.Auth;
using AskMe.Application.Questions;
using AskMe.Application.Users;
using AskMe.Application.Votes;
using AskMe.Infrastructure;
using AskMe.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ---- Forwarded headers: almost every free/PaaS host (Render, Railway, Fly,
// ...) puts the app behind a reverse proxy, so Kestrel sees the proxy's IP as
// the "remote" address unless told to trust X-Forwarded-For/-Proto. Without
// this, every request looks like it came from the same internal IP - which
// is why Source IP could show up wrong/missing on a real deployment even
// though it worked correctly on localhost.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Most free-tier hosts don't expose a fixed, documented proxy IP/CIDR
    // range to whitelist, so we trust any proxy in front of us. This is a
    // reasonable trade-off ONLY because the host's edge network is what's
    // actually terminating the public connection (Kestrel is never directly
    // internet-facing here) - don't do this if you're not confident that's
    // true for your hosting setup.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ---- Services ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

// Application-layer use-case services registered directly (no MediatR - kept
// simple and explicit for an MVP-scale codebase).
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<QuestionService>();
builder.Services.AddScoped<VoteService>();
builder.Services.AddScoped<AdminService>();

// ---- Authentication ----
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret must be configured via environment variable/secret store.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        ClockSkew = TimeSpan.FromMinutes(1),
    };
});

builder.Services.AddAuthorization();

// ---- CORS: only the configured frontend origin, never a wildcard, since
// cookies/auth headers and user-generated content are involved. ----
var allowedOrigin = builder.Configuration["AllowedFrontendOrigin"]
    ?? throw new InvalidOperationException("AllowedFrontendOrigin must be configured.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ---- Rate limiting for question submission and voting, per requirement.
// Keyed by client IP so it can't be trivially bypassed by omitting a session. ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("submit", ctx => RateLimitPartition.GetSlidingWindowLimiter(
        GetClientIp(ctx), _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            QueueLimit = 0,
        }));

    options.AddPolicy("vote", ctx => RateLimitPartition.GetSlidingWindowLimiter(
        GetClientIp(ctx), _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            QueueLimit = 0,
        }));

    options.AddPolicy("auth", ctx => RateLimitPartition.GetSlidingWindowLimiter(
        GetClientIp(ctx), _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            QueueLimit = 0,
        }));
});

static string GetClientIp(HttpContext ctx)
{
    if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var f) && !string.IsNullOrWhiteSpace(f))
        return f.ToString().Split(',')[0].Trim();
    return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

var app = builder.Build();

// ---- Pipeline ----
app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Apply pending EF Core migrations automatically on startup - convenient for
// free-tier hosting where running a separate migration step isn't always easy.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

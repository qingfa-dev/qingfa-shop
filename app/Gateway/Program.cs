using System.Text;
using System.Threading.RateLimiting;

using Gateway;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

using ServiceDefaults;

using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Initialize: token validation only. Issuing tokens is a later feature, so the
// signing key is a development validation key from appsettings.json.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        string key = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key must be configured.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        };
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy(
        GatewayRouter.ProtectedApiPolicy,
        policy => policy.RequireAuthenticatedUser()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Handle: partition per client address so one caller cannot exhaust the edge
    // for everyone else.
    options.AddPolicy(
        GatewayRouter.RateLimitPolicy,
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0,
            }));
});

builder.Services.AddReverseProxy()
    .LoadFromMemory(GatewayRouter.Routes(), GatewayRouter.Clusters(builder.Configuration))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Handle: the gateway's own liveness/readiness endpoints. They are mapped as
// first-class ASP.NET endpoints, so the proxy's catch-all route never sees them.
app.MapDefaultEndpoints();

// Validate: rate limiting is attached to the proxy endpoint only, so health probes
// are not throttled and a kubelet cannot be locked out by a busy client.
app.MapReverseProxy().RequireRateLimiting(GatewayRouter.RateLimitPolicy);

app.Run();
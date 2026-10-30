using System.Text;

using Application.Common.Security.Authentication.Contexts;
using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth;
using Application.Features.Identity.Auth.Abstractions;
using Application.Features.Identity.External.Abstractions;
using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Abstractions.Blacklist;
using Application.Common.Security.Authentication.Tokens.Abstractions.Jwt;
using Application.Common.Security.Authentication.Tokens.Abstractions.Refresh;
using Application.Common.Security.Authentication.Tokens.Abstractions.Rotation;
using Application.Common.Security.Authentication.Tokens.Options;

using Infrastructure.Security.Authentication.Contexts;
using Infrastructure.Security.Authentication.Tokens.Services;
using Infrastructure.Security.Authentication.Tokens.Stores;
using Infrastructure.Security.Identity;
using Infrastructure.Security.Identity.Services;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security.Authentication;

/// <summary>
/// Registers the authentication contexts and token services used by the application.
/// </summary>
public static class AuthenticationComposition
{
    /// <summary>
    /// Adds request contexts, token options, JWT issuance, refresh-token handling,
    /// blacklisting, and rotation.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configuration">Configuration holding the token sections.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configuration"/> is <c>null</c>.
    /// </exception>
    public static WebApplicationBuilder AddAuthentication(
        this WebApplicationBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        IServiceCollection services = builder.Services;

        services.AddHttpContextAccessor();
        services.TryAddScoped<ITenantContext, TenantContext>();
        services.TryAddScoped<IUserContext, CurrentUser>();
        services.AddIdentityServices();

        // Initialize: keep the system clock replaceable so tests can inject a fake.
        services.TryAddSingleton(TimeProvider.System);

        JwtTokenOption jwt = configuration
            .GetSection(TokenConstant.Sections.Jwt)
            .Get<JwtTokenOption>() ?? new JwtTokenOption();

        services.TryAddSingleton(jwt);
        services.TryAddSingleton(
            configuration
                .GetSection(TokenConstant.Sections.RefreshToken)
                .Get<RefreshTokenOption>() ?? new RefreshTokenOption());
        services.TryAddSingleton(
            configuration
                .GetSection(TokenConstant.Sections.Blacklist)
                .Get<TokenBlacklistOption>() ?? new TokenBlacklistOption());

        // Register: distributed blacklist storage falls back to memory when no L2 cache is configured.
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IDistributedCache)))
            services.AddDistributedMemoryCache();

        services.TryAddScoped<IRefreshTokenStore, EfCoreRefreshTokenStore>();
        services.TryAddSingleton<ITokenBlacklistStore, DistributedTokenBlacklistStore>();

        services.TryAddSingleton<ITokenService, JwtTokenService>();
        services.TryAddScoped<IRefreshTokenService, RefreshTokenService>();
        services.TryAddScoped<ITokenBlacklist, TokenBlacklistService>();
        services.TryAddScoped<ITokenRotationService, TokenRotationService>();

        IdentityApiOption identity = configuration
            .GetSection(IdentityConstant.Defaults.SectionName)
            .Get<IdentityApiOption>() ?? new IdentityApiOption();
        ExternalAuthOption external = configuration
            .GetSection(IdentityConstant.Defaults.ExternalSectionName)
            .Get<ExternalAuthOption>() ?? new ExternalAuthOption();

        services.TryAddSingleton(identity);
        services.TryAddSingleton(external);
        services.TryAddScoped<IdentityTokenIssuer>();
        services.TryAddScoped<IIdentityService, IdentityService>();
        services.TryAddScoped<IExternalAuthenticationService, ExternalAuthenticationService>();
        services.TryAddScoped<IIdentityEmailSender, LoggingIdentityEmailSender>();

        AuthenticationBuilder authentication = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        if (!string.IsNullOrWhiteSpace(jwt.Key))
        {
            authentication.AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds)
                };
            });
        }

        if (external.Google.Enabled)
        {
            authentication.AddGoogle(options =>
            {
                options.ClientId = external.Google.ClientId;
                options.ClientSecret = external.Google.ClientSecret;
                options.CallbackPath = IdentityConstant.Routes.ExternalGoogleCallback;
            });
        }

        if (external.Microsoft.Enabled)
        {
            authentication.AddMicrosoftAccount(options =>
            {
                options.ClientId = external.Microsoft.ClientId;
                options.ClientSecret = external.Microsoft.ClientSecret;
                options.CallbackPath = IdentityConstant.Routes.ExternalMicrosoftCallback;
            });
        }

        if (external.OpenIdConnect.Enabled)
        {
            authentication.AddOpenIdConnect(options =>
            {
                options.Authority = external.OpenIdConnect.Authority;
                options.ClientId = external.OpenIdConnect.ClientId;
                options.ClientSecret = external.OpenIdConnect.ClientSecret;

                // Fallback: keep a configured callback path, otherwise use the route the API exposes.
                options.CallbackPath = string.IsNullOrWhiteSpace(external.OpenIdConnect.CallbackPath)
                    ? IdentityConstant.Routes.ExternalOpenIdConnectCallback
                    : external.OpenIdConnect.CallbackPath;

                options.ResponseType = "code";
                options.SaveTokens = true;
                options.Scope.Clear();
                foreach (string scope in external.OpenIdConnect.Scopes)
                    options.Scope.Add(scope);
            });
        }

        services.AddAuthenticationResultContract();

        return builder;
    }

    /// <summary>Adds the authentication middleware to the request pipeline.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseAuthentication(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Call: the framework middleware registered by AddAuthentication.
        app.UseAuthentication();
        return app;
    }
}

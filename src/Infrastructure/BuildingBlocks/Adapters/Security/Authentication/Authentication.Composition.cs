using System.Text;

using Application.BuildingBlocks.Security.Authentication.Contexts;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Auth;
using Application.BuildingBlocks.Security.Identity.Auth.Abstractions;
using Application.BuildingBlocks.Security.Identity.Auth.Options;
using Application.BuildingBlocks.Security.Identity.External.Abstractions;
using Application.BuildingBlocks.Security.Identity.External.Options;
using Application.BuildingBlocks.Security.Authentication.Tokens;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Blacklist;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Jwt;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Refresh;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Rotation;
using Application.BuildingBlocks.Security.Authentication.Tokens.Options;
using Application.BuildingBlocks.UseCases.Validation.FluentValidation;

using FluentValidation;

using Infrastructure.BuildingBlocks.Adapters.Security.Authentication.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Security.Authentication.Tokens.Services;
using Infrastructure.BuildingBlocks.Adapters.Security.Authentication.Tokens.Stores;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Services;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Authentication;

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

        // Validate: token options must satisfy their guards when the host starts.
        services.AddOptions<JwtTokenOption>()
            .Bind(configuration.GetSection(TokenConstant.Sections.Jwt))
            .Validate(
                options => string.IsNullOrWhiteSpace(options.Key) ||
                    TokenOptionValidator.Validate(options).IsSuccess,
                "JWT token options are invalid.")
            .ValidateOnStart();
        services.AddOptions<RefreshTokenOption>()
            .Bind(configuration.GetSection(TokenConstant.Sections.RefreshToken))
            .Validate(
                options => TokenOptionValidator.Validate(options).IsSuccess,
                "Refresh token options are invalid.")
            .ValidateOnStart();
        services.AddOptions<TokenBlacklistOption>()
            .Bind(configuration.GetSection(TokenConstant.Sections.Blacklist))
            .Validate(
                options => TokenOptionValidator.Validate(options).IsSuccess,
                "Token blacklist options are invalid.")
            .ValidateOnStart();

        JwtTokenOption jwt = configuration
            .GetSection(TokenConstant.Sections.Jwt)
            .Get<JwtTokenOption>() ?? new JwtTokenOption();

        services.TryAddSingleton(provider =>
            provider.GetRequiredService<IOptions<JwtTokenOption>>().Value);
        services.TryAddSingleton(provider =>
            provider.GetRequiredService<IOptions<RefreshTokenOption>>().Value);
        services.TryAddSingleton(provider =>
            provider.GetRequiredService<IOptions<TokenBlacklistOption>>().Value);

        // Register: distributed blacklist storage falls back to memory when no L2 cache is configured.
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IDistributedCache)))
            services.AddDistributedMemoryCache();

        services.TryAddScoped<IRefreshTokenStore, EfCoreRefreshTokenStore>();
        services.TryAddSingleton<ITokenBlacklistStore, DistributedTokenBlacklistStore>();

        services.TryAddSingleton<ITokenService, JwtTokenService>();
        services.TryAddScoped<IRefreshTokenService, RefreshTokenService>();
        services.TryAddScoped<ITokenBlacklist, TokenBlacklistService>();
        services.TryAddScoped<ITokenRotationService, TokenRotationService>();

        // Validate: identity API URLs, paths, and the default role must be usable at startup.
        services.AddSingleton<IValidator<IdentityApiOption>, IdentityOptionValidator>();
        services.AddOptions<IdentityApiOption>()
            .Bind(configuration.GetSection(IdentityConstant.Defaults.SectionName))
            .ValidateFluentValidation()
            .ValidateOnStart();

        // Validate: enabled external providers must carry complete client settings.
        services.AddSingleton<IValidator<ExternalAuthOption>, ExternalOptionValidator>();
        services.AddOptions<ExternalAuthOption>()
            .Bind(configuration.GetSection(IdentityConstant.Defaults.ExternalSectionName))
            .ValidateFluentValidation()
            .ValidateOnStart();

        ExternalAuthOption external = configuration
            .GetSection(IdentityConstant.Defaults.ExternalSectionName)
            .Get<ExternalAuthOption>() ?? new ExternalAuthOption();

        services.TryAddSingleton(provider =>
            provider.GetRequiredService<IOptions<IdentityApiOption>>().Value);
        services.TryAddSingleton(provider =>
            provider.GetRequiredService<IOptions<ExternalAuthOption>>().Value);
        services.TryAddScoped<IdentityTokenIssuer>();
        services.TryAddScoped<IIdentityService, IdentityService>();
        services.TryAddScoped<IUserAccountService, UserAccountService>();
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

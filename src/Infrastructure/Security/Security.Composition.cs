using Application.Common.Security.Authentication.Contexts;
using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Options;
using Application.Common.Security.Authentication.Tokens.Services;

using Infrastructure.Security.Authentication.Contexts;
using Infrastructure.Security.Authentication.Tokens;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Security;

/// <summary>
/// Registers the HTTP-backed request contexts and token services that application handlers depend on.
/// </summary>
public static class SecurityComposition
{
    /// <summary>
    /// Adds the current tenant and user contexts plus the JWT token service.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="configuration">Configuration holding the token sections.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="configuration"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpContextAccessor();
        services.TryAddScoped<ITenantContext, TenantContext>();
        services.TryAddScoped<IUserContext, CurrentUser>();

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

        services.TryAddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}

using Application.Common.Security.Authentication.Contexts;

using Infrastructure.Security.Authentication.Contexts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Security;

/// <summary>
/// Registers the HTTP-backed request contexts that application handlers depend on.
/// </summary>
public static class SecurityComposition
{
    /// <summary>
    /// Adds the current tenant and user context implementations.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.TryAddScoped<ITenantContext, TenantContext>();
        services.TryAddScoped<IUserContext, CurrentUser>();

        return services;
    }
}

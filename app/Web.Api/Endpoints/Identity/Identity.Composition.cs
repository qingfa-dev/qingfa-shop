using Application.Features.Identity.External.Abstractions;

using Infrastructure.Integrations.Security;

using Microsoft.Extensions.DependencyInjection;

namespace Web.Api.Endpoints.Identity;

/// <summary>Registers the identity feature's scoped collaborators.</summary>
public static class IdentityComposition
{
    /// <summary>Adds the external one-time-code store.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddIdentityFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IExternalCodeStore, ExternalCodeStore>();
        return services;
    }
}

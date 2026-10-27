using System.Reflection;

using Application.Common.Contexts;
using Application.Common.Mediators;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Application;

/// <summary>
/// Composition root for the Application layer.
/// Registers all application services: MediatR, validators, and object mapping.
/// </summary>
public static class ApplicationComposition
{
    /// <summary>
    /// Adds all application services to the service collection.
    /// This includes MediatR with pipeline behaviors and FluentValidation integration.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register: MediatR with pipeline behaviors (logging, validation, exception mapping).
        services.AddMediators();

        // Register: the scoped ambient context the host populates per request.
        services.TryAddScoped<AmbientExecutionContext>();
        services.TryAddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientExecutionContext>());
        services.TryAddScoped<IActorContext>(sp => sp.GetRequiredService<AmbientExecutionContext>());

        return services;
    }

    /// <summary>
    /// Adds all application services with additional assemblies to scan for handlers and validators.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="additionalAssemblies">Additional assemblies to scan for handlers and validators.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        params Assembly[] additionalAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register: MediatR with pipeline behaviors (logging, validation, exception mapping).
        services.AddMediators(additionalAssemblies);

        // Register: the scoped ambient context the host populates per request.
        services.TryAddScoped<AmbientExecutionContext>();
        services.TryAddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientExecutionContext>());
        services.TryAddScoped<IActorContext>(sp => sp.GetRequiredService<AmbientExecutionContext>());

        return services;
    }

    #region Pipeline Configuration

    /// <summary>
    /// Initializes Application layer services at startup.
    /// Warms up MediatR and validates the service configuration.
    /// Call this after building the host to verify services are resolvable.
    /// </summary>
    /// <param name="provider">The application's service provider.</param>
    /// <returns>The same provider for chaining.</returns>
    public static IServiceProvider UseApplication(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        // Warm up: MediatR services to catch configuration errors early.
        provider.UseMediators();

        return provider;
    }

    #endregion
}

using System.Reflection;

using Application.Common.Mediators.Behaviors.Exceptions;
using Application.Common.Mediators.Behaviors.Logging;
using Application.Common.Mediators.Behaviors.Validation;

using FluentValidation;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace Application.Common.Mediators;

/// <summary>
/// Provides extension methods for configuring MediatR and associated behaviors in the dependency injection container.
/// </summary>
public static class MediatorComposition
{
    #region Service Registration

    /// <summary>
    /// Registers MediatR services along with logging, validation, and exception mapping behaviors.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="additionalAssemblies">Additional assemblies to scan for validators and handlers.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddMediators(
        this IServiceCollection services,
        params Assembly[] additionalAssemblies)
    {
        #region MediatR Configuration

        // Initialize: register the validators the validation behavior resolves.
        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        foreach (var assembly in additionalAssemblies)
        {
            services.AddValidatorsFromAssembly(assembly);
        }

        // Initialize: Configure MediatR with pipeline behaviors
        services.AddMediatR(cfg =>
        {
            // Initialize: Scoped is mandatory. AddMediatR defaults to Singleton, which
            // captures scoped dependencies inside handlers and fails on first use.
            cfg.Lifetime = ServiceLifetime.Scoped;

            // Register: Handlers from the current building block assembly
            cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly);

            // Register: Handlers from additional assemblies (e.g., Module)
            foreach (var assembly in additionalAssemblies)
            {
                cfg.RegisterServicesFromAssembly(assembly);
            }

            // Add: Pipeline behaviors in priority order (Outer to Inner)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        });

        #endregion

        return services;
    }

    #endregion

    #region Pipeline Configuration

    /// <summary>
    /// Initializes MediatR services for the application, ensuring services are warmed up.
    /// </summary>
    /// <param name="provider">The service provider to configure.</param>
    /// <returns>The service provider for method chaining.</returns>
    public static IServiceProvider UseMediators(this IServiceProvider provider)
    {
        #region Service Warming

        // Resolve: warm up the mediator inside a scope. MediatR is scoped, and a
        // web host validates that scoped services are not resolved from the root.
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMediator>();

        #endregion

        return provider;
    }

    #endregion
}
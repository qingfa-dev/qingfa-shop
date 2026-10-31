using System.Reflection;

using Application.BuildingBlocks.UseCases.Mediation.Behaviors.Exceptions;
using Application.BuildingBlocks.UseCases.Mediation.Behaviors.Logging;
using Application.BuildingBlocks.UseCases.Mediation.Behaviors.Transactions;
using Application.BuildingBlocks.UseCases.Mediation.Behaviors.Validation;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Application.BuildingBlocks.UseCases.Mediation;

/// <summary>
/// Provides extension methods for configuring MediatR and associated behaviors in the dependency injection container.
/// </summary>
public static class MediatorComposition
{
    #region Service Registration

    /// <summary>
    /// Registers MediatR services along with logging, validation, and exception mapping behaviors.
    /// </summary>
    /// <param name="builder">The host builder to add services to.</param>
    /// <param name="additionalAssemblies">Additional assemblies to scan for validators and handlers.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddMediators(
        this WebApplicationBuilder builder,
        params Assembly[] additionalAssemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IServiceCollection services = builder.Services;

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
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        });

        #endregion

        return builder;
    }

    #endregion

    #region Pipeline Configuration

    /// <summary>
    /// Initializes MediatR services for the application, ensuring services are warmed up.
    /// </summary>
    /// <param name="app">The host application to configure.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseMediators(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        #region Service Warming

        // Resolve: warm up the mediator inside a scope. MediatR is scoped, and a
        // web host validates that scoped services are not resolved from the root.
        using IServiceScope scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMediator>();

        #endregion

        return app;
    }

    #endregion
}
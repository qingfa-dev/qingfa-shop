using System.Reflection;

using Application.Common.Mediators;

using FluentValidation;

using Microsoft.AspNetCore.Builder;

namespace Application;

/// <summary>
/// Composition root for the Application layer.
/// Registers all application services: MediatR, validators, and object mapping.
/// </summary>
public static class ApplicationComposition
{
    /// <summary>
    /// Adds all application services to the web application builder.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="additionalAssemblies">Additional assemblies to scan for handlers and validators.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddApplication(
        this WebApplicationBuilder builder,
        params Assembly[] additionalAssemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register: MediatR with pipeline behaviors (logging, validation, exception mapping).
        builder.AddMediators(additionalAssemblies);

        // Subscribe: register validators from the host and this assembly.
        builder.Services.AddValidatorsFromAssembly(Assembly.GetEntryAssembly());
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (Assembly assembly in additionalAssemblies)
            builder.Services.AddValidatorsFromAssembly(assembly);

        return builder;
    }

    /// <summary>
    /// Initializes Application layer services at startup.
    /// Warms up MediatR and validates the service configuration.
    /// </summary>
    /// <param name="app">The built web application.</param>
    /// <returns>The same application for chaining.</returns>
    public static WebApplication UseApplication(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Warm up: MediatR services to catch configuration errors early.
        app.UseMediators();

        return app;
    }
}

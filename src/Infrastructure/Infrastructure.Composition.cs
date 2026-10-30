using Application;
using Application.Common.Persistence.Initialization;

using Infrastructure.Caching;
using Infrastructure.Integrations;
using Infrastructure.Persistence;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.EventSourcing;
using Infrastructure.Persistence.EntityFramework.Outbox;
using Infrastructure.Security;
using Infrastructure.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer.
/// Registers all infrastructure services: persistence, caching, repositories, and query maps.
/// </summary>
public static class InfrastructureComposition
{
    /// <summary>
    /// Adds all infrastructure services to the web application builder.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(builder.Configuration);

        // Register: Persistence services (EF Core, repositories, query maps).
        builder.AddPersistence<ApplicationDbContext>(builder.Configuration);

        // Register: Caching facade (HybridCache over L1/L2 tiers).
        builder.AddCaching(builder.Configuration);

        // Register: Request-scoped tenant/user contexts and token services.
        builder.AddSecurity(builder.Configuration);

        // Register: Cross-aggregate reference cleanup.
        builder.AddServices();

        // Register: durable outbox dispatcher, worker, inbox, and health check.
        builder.AddOutbox(builder.Configuration);
        builder.AddEventStore(builder.Configuration);

        // Register: outbound integrations, observability, background jobs, and OpenAPI.
        builder.AddIntegration();

        return builder;
    }

    /// <summary>
    /// Initializes the infrastructure pipeline at startup.
    /// Warms application services, then applies migrations/seeding when the
    /// <c>Persistence:AutoMigrate</c> or <c>Persistence:Seed</c> options are enabled.
    /// </summary>
    /// <param name="app">The built web application.</param>
    /// <returns>The same application for further pipeline configuration.</returns>
    /// <exception cref="InvalidOperationException">Initialization returned a failure.</exception>
    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Warm up: application services are part of the same startup pipeline.
        app.UseApplication();
        app.UseCaching();
        app.UsePersistence();
        app.UsePersistenceInitialization();
        app.UseServices();
        app.UseIntegration();

        bool autoMigrate = app.Configuration.GetValue("Persistence:AutoMigrate", false);
        bool seed = app.Configuration.GetValue("Persistence:Seed", false);

        if (!autoMigrate && !seed)
        {
            return app;
        }

        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IPersistenceInitializer>();
        var result = initializer
            .InitializeAsync(runMigrations: autoMigrate, runSeeders: seed)
            .GetAwaiter()
            .GetResult();

        if (result.IsFailure)
        {
            // Boundary: startup initialization failed; the host must not start with a broken database.
            throw new InvalidOperationException(
                $"Database initialization failed: {string.Join("; ", result.Errors.Select(error => error.Message))}");
        }

        return app;
    }
}

using Application;
using Application.BuildingBlocks.Persistence.Initialization;

using Infrastructure.BuildingBlocks;
using Infrastructure.Modules;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

/// <summary>Composition root for the Infrastructure layer.</summary>
public static class InfrastructureComposition
{
    /// <summary>Registers BuildingBlocks and the business modules.</summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(builder.Configuration);

        InfrastructureModuleRegistrations modules =
            ModularInfraComposition.CreateRegistrations();

        builder.AddBuildingBlockInfrastructure(builder.Configuration, modules);
        builder.AddModularInfrastructure();
        return builder;
    }

    /// <summary>Initializes the Infrastructure pipeline.</summary>
    /// <param name="app">The built web application.</param>
    /// <returns>The same application for further pipeline configuration.</returns>
    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseApplication();
        app.UseBuildingBlockInfrastructure();
        app.UseModularInfrastructure();

        bool autoMigrate = app.Configuration.GetValue("Persistence:AutoMigrate", false);
        bool seed = app.Configuration.GetValue("Persistence:Seed", false);

        if (!autoMigrate && !seed)
            return app;

        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IPersistenceInitializer>();
        var result = initializer
            .InitializeAsync(runMigrations: autoMigrate, runSeeders: seed)
            .GetAwaiter()
            .GetResult();

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database initialization failed: {string.Join("; ", result.Errors.Select(error => error.Message))}");
        }

        return app;
    }
}

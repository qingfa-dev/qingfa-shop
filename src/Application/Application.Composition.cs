using System.Reflection;

using Application.BuildingBlocks;
using Application.Modules;

using Microsoft.AspNetCore.Builder;

namespace Application;

/// <summary>Composition root for the Application layer.</summary>
public static class ApplicationComposition
{
    /// <summary>Adds the reusable BuildingBlocks and the business modules.</summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="additionalAssemblies">Additional assemblies to scan.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddApplication(
        this WebApplicationBuilder builder,
        params Assembly[] additionalAssemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddBuildingBlockApplication(additionalAssemblies);
        builder.AddModularApplication();
        return builder;
    }

    /// <summary>Initializes the Application layer.</summary>
    /// <param name="app">The built web application.</param>
    /// <returns>The same application for further pipeline configuration.</returns>
    public static WebApplication UseApplication(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseBuildingBlockApplication();
        app.UseModularApplication();
        return app;
    }
}

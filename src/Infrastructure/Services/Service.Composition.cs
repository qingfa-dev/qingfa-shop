using Application.Features.Todos.Abstractions;

using Infrastructure.Services.Todos;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Services;

/// <summary>Registers cross-aggregate infrastructure services.</summary>
public static class ServiceComposition
{
    /// <summary>Adds cross-aggregate services to the host.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Create: Cross-aggregate reference cleanup used by the delete use cases.
        builder.Services.TryAddScoped<ITodoReferenceCleaner, TodoReferenceCleaner>();
        return builder;
    }

    /// <summary>Adds the cross-aggregate service pipeline hook for the host.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseServices(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app;
    }
}

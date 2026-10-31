using Application.BuildingBlocks.Security.Authentication.Contexts;

using Carter;

using Infrastructure.BuildingBlocks.Adapters.Serialization;

using ServiceDefaults;

using Web.Api.Endpoints.Identity;
using Web.Api.Endpoints.Payments;
using Web.Api.Endpoints.Todos;

namespace Web.Api.Composition;

/// <summary>Composes the HTTP surface: JSON, Carter modules, and endpoint helpers.</summary>
public static class ApiComposition
{
    /// <summary>Registers the API feature modules and HTTP conventions.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddApiJsonOptions();
        builder.Services.AddProblemDetails();
        builder.Services.AddIdentityFeature();

        builder.Services.AddCarter(configurator: configurator =>
        {
            configurator.WithModule<IdentityEndpoint>();
            configurator.WithModule<IdentityUserEndpoint>();
            configurator.WithModule<IdentityRoleEndpoint>();
            configurator.WithModule<IdentityPermissionEndpoint>();
            configurator.WithModule<TodoEndpoint>();
            configurator.WithModule<TodoListEndpoint>();
            configurator.WithModule<TodoCategoryEndpoint>();
            configurator.WithModule<TodoReminderEndpoint>();
            configurator.WithModule<PaymentEndpoint>();
            configurator.WithModule<PaymentAdminEndpoint>();
            configurator.WithModule<PaymentWebhookEndpoint>();
        });

        return builder;
    }

    /// <summary>Maps the API endpoints.</summary>
    /// <param name="app">The built application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.MapCarter();

        // Handle: /time exposes the injected TimeProvider so integration tests can prove
        // the fake clock is wired through DI rather than the endpoint reading the system clock.
        app.MapGet("/time", (TimeProvider clock) => Results.Json(new { utcNow = clock.GetUtcNow() }));

        return app;
    }
}

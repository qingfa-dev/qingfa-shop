using Infrastructure.BuildingBlocks;
using Infrastructure.Modules.Payments.Persistence;
using Infrastructure.Modules.Todos.Integrations.Messaging;
using Infrastructure.Modules.Todos.Persistence.EventSourcing;
using Infrastructure.Modules.Todos.Services;

using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Modules;

/// <summary>Composition root for Infrastructure business modules.</summary>
public static class ModularInfraComposition
{
    /// <summary>Creates the module contributions consumed by BuildingBlocks registration.</summary>
    /// <returns>The module registration set.</returns>
    public static InfrastructureModuleRegistrations CreateRegistrations() =>
        new(
            Messaging: [new TodoMessagingModule()],
            EventStores: [new TodoEventStoreModule(), new PaymentEventStoreModule()]);

    /// <summary>Registers module services.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddModularInfrastructure(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddTodoServices();
        return builder;
    }

    /// <summary>Adds module pipeline hooks.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseModularInfrastructure(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }
}

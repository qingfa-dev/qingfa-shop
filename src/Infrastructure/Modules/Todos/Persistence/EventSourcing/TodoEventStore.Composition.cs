using Application.BuildingBlocks.EventSourcing;

using Core.BuildingBlocks.Domain.EventSourcing;
using Core.Modules.Todos.Items;
using Core.Modules.Todos.Reminders;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.Modules.Todos.Persistence.EventSourcing.Options;
using Infrastructure.Modules.Todos.Persistence.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Modules.Todos.Persistence.EventSourcing;

/// <summary>Registers the Todo aggregates, factories, and projections.</summary>
public sealed class TodoEventStoreModule : IEventStoreModule
{
    /// <inheritdoc />
    public void Configure(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<TodoEventStoreSetting>()
            .BindConfiguration(TodoEventStoreSetting.SectionName);

        services.TryAddScoped<IAggregateFactory<TodoReminder>, TodoReminderFactory>();
        services.TryAddScoped<IAggregateFactory<Todo>, TodoFactory>();
        services.TryAddScoped<IProjection, TodoReminderProjection>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IProjection, TodoProjection>());
    }
}

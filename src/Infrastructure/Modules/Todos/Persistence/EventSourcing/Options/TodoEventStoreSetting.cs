using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;

namespace Infrastructure.Modules.Todos.Persistence.EventSourcing.Options;

/// <summary>Todo-specific event-store switches that share the generic EventStore section.</summary>
public sealed class TodoEventStoreSetting
{
    /// <summary>The shared EventStore configuration section.</summary>
    public const string SectionName = EventStoreSetting.SectionName;

    /// <summary>Gets or sets whether the Todo backfill seeder runs.</summary>
    public bool BackfillTodos { get; set; }
}

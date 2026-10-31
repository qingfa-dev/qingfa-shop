using Infrastructure.BuildingBlocks.Adapters.Messaging;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

namespace Infrastructure.BuildingBlocks;

/// <summary>Module contributions consumed by the BuildingBlocks composition.</summary>
/// <param name="Messaging">Messaging topology contributors.</param>
/// <param name="EventStores">Event-store aggregate/projection contributors.</param>
public sealed record InfrastructureModuleRegistrations(
    IReadOnlyList<IMessagingModule> Messaging,
    IReadOnlyList<IEventStoreModule> EventStores)
{
    /// <summary>An empty contribution set.</summary>
    public static InfrastructureModuleRegistrations Empty { get; } = new([], []);
}

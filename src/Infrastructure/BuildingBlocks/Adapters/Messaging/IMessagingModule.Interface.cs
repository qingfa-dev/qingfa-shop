using Infrastructure.BuildingBlocks.Adapters.Messaging.Options;
using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;

using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Infrastructure.BuildingBlocks.Adapters.Messaging;

/// <summary>Contributes module-specific exchange, queue, and handler wiring to Wolverine.</summary>
public interface IMessagingModule
{
    /// <summary>Registers the module's event identities independently of the transport.</summary>
    /// <param name="eventTypes">The shared event-type registry.</param>
    /// <remarks>
    /// Identity registration runs even when messaging is disabled: the outbox still stages
    /// integration rows and its dispatcher resolves them through the registry.
    /// </remarks>
    void RegisterEventTypes(EventTypeRegistry eventTypes);

    /// <summary>Configures the module's Wolverine topology.</summary>
    /// <param name="options">The Wolverine options.</param>
    /// <param name="setting">The bound messaging settings.</param>
    /// <param name="eventTypes">The shared event-type registry.</param>
    /// <param name="transport">The RabbitMQ transport, or <c>null</c> for the in-memory transport.</param>
    void Configure(
        WolverineOptions options,
        MessagingSetting setting,
        EventTypeRegistry eventTypes,
        RabbitMqTransportExpression? transport);
}

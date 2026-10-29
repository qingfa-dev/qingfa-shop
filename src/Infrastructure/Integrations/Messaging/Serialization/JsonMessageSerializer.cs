using System.Text.Json;
using System.Text.Json.Serialization;

using Application.Common.Messaging.Bus;
using Application.Common.Messaging.Serialization;

using Core.Common.Domain.Events;
using Core.Common.Monad.Metadata;

namespace Infrastructure.Integrations.Messaging.Serialization;

/// <summary>Serializes domain and integration events to a stable JSON envelope.</summary>
/// <remarks>
/// Payloads use <see cref="JsonSerializerDefaults.Web"/> so properties are camel-cased, enums are
/// written as names, and metadata travels as a flat object map. The event name and version are
/// carried by the outbox envelope rather than repeated inside the payload.
/// </remarks>
public sealed class JsonMessageSerializer : IDomainEventSerializer, IIntegrationEventSerializer
{
    private readonly IEventTypeRegistry _registry;
    private readonly JsonSerializerOptions _options;

    /// <summary>Creates a JSON serializer that resolves event types through the registry.</summary>
    /// <param name="registry">The registry used to resolve event names and versions.</param>
    public JsonMessageSerializer(IEventTypeRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));

        _options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _options.Converters.Add(new JsonStringEnumConverter());
        _options.Converters.Add(new MetadataJsonConverter());
    }

    /// <inheritdoc />
    public string Serialize(IMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        // Serialize by runtime type: the interface itself carries no members.
        return JsonSerializer.Serialize(metadata, metadata.GetType(), _options);
    }

    /// <inheritdoc />
    public string Serialize(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _options);
    }

    /// <inheritdoc />
    public string Serialize(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), _options);
    }

    // Explicit: both contracts differ only by return type, so implicit implementation would collide.
    IDomainEvent IDomainEventSerializer.Deserialize(string eventName, int eventVersion, string payload)
        => (IDomainEvent)Deserialize(ResolveDomainType(eventName, eventVersion), payload);

    IIntegrationEvent IIntegrationEventSerializer.Deserialize(string eventName, int eventVersion, string payload)
        => (IIntegrationEvent)Deserialize(_registry.Resolve(eventName, eventVersion), payload);

    private object Deserialize(Type eventType, string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        // Fail: a null payload means the stored outbox row cannot be replayed.
        return JsonSerializer.Deserialize(payload, eventType, _options)
            ?? throw new InvalidOperationException(
                $"The payload for '{eventType.FullName}' deserialized to null.");
    }

    private Type ResolveDomainType(string eventName, int eventVersion)
    {
        // Fallback: assembly-qualified names resolve without scanning; the registry handles the rest.
        return Type.GetType(eventName, throwOnError: false) ?? _registry.Resolve(eventName, eventVersion);
    }
}

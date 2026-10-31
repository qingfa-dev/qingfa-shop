using Core.BuildingBlocks.Modeling.Metadata;

namespace Application.BuildingBlocks.EventSourcing;

/// <summary>A persisted event with its stream and global positions.</summary>
/// <param name="StreamId">The stream identifier.</param>
/// <param name="Version">The stream version.</param>
/// <param name="EventId">The unique event identifier.</param>
/// <param name="EventName">The stable event name.</param>
/// <param name="EventVersion">The payload version.</param>
/// <param name="Payload">The serialized payload.</param>
/// <param name="Metadata">The serialized metadata envelope.</param>
/// <param name="OccurredOnUtc">The event timestamp.</param>
/// <param name="Position">The global position.</param>
public sealed record StoredEvent(
    string StreamId,
    long Version,
    Guid EventId,
    string EventName,
    int EventVersion,
    string Payload,
    DefaultMetadata Metadata,
    DateTimeOffset OccurredOnUtc,
    long Position);

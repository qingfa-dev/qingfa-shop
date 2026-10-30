namespace Application.Common.EventSourcing;

/// <summary>A point-in-time aggregate state captured to shorten replay.</summary>
/// <param name="StreamId">The stream identifier.</param>
/// <param name="Version">The stream version the state represents.</param>
/// <param name="State">The serialized aggregate state.</param>
/// <param name="TakenOnUtc">The capture timestamp.</param>
public sealed record Snapshot(
    string StreamId,
    long Version,
    string State,
    DateTimeOffset TakenOnUtc);

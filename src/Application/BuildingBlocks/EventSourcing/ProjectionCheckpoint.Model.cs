namespace Application.BuildingBlocks.EventSourcing;

/// <summary>The last global position processed by one projection.</summary>
/// <param name="Projection">The projection name.</param>
/// <param name="Position">The last processed global position.</param>
/// <param name="UpdatedOnUtc">The checkpoint update timestamp.</param>
public sealed record ProjectionCheckpoint(
    string Projection,
    long Position,
    DateTimeOffset UpdatedOnUtc);

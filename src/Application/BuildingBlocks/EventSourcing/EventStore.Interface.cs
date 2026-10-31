using Core.BuildingBlocks.Domain.Events;

namespace Application.BuildingBlocks.EventSourcing;

/// <summary>Appends and reads durable event streams.</summary>
public interface IEventStore
{
    /// <summary>Appends events at the expected stream version.</summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="expectedVersion">The version the caller last observed; 0 for a new stream.</param>
    /// <param name="events">The events to append in order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new stream version, or a stable append failure.</returns>
    Task<Result<long>> AppendAsync(
        string streamId,
        long expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken = default);

    /// <summary>Reads a stream from the given version.</summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="fromVersion">The first version to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored events in version order.</returns>
    Task<Result<IReadOnlyList<StoredEvent>>> ReadStreamAsync(
        string streamId,
        long fromVersion,
        CancellationToken cancellationToken = default);

    /// <summary>Reads all streams in global position order.</summary>
    /// <param name="fromPosition">The first global position to read.</param>
    /// <param name="batchSize">The maximum number of rows.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored events in position order.</returns>
    Task<Result<IReadOnlyList<StoredEvent>>> ReadAllAsync(
        long fromPosition,
        int batchSize,
        CancellationToken cancellationToken = default);
}

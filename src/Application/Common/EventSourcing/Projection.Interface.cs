namespace Application.Common.EventSourcing;

/// <summary>A read-model builder driven by the global event position.</summary>
public interface IProjection
{
    /// <summary>Gets the stable projection name.</summary>
    string Name { get; }

    /// <summary>Applies one stored event idempotently.</summary>
    /// <param name="storedEvent">The event and its positions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or a stable projection failure.</returns>
    Task<Result> ApplyAsync(StoredEvent storedEvent, CancellationToken cancellationToken = default);
}

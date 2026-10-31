namespace Application.BuildingBlocks.Messaging.Outbox;

/// <summary>Dispatches one batch of outbox messages.</summary>
public interface IOutboxDispatcher
{
    Task<Result<int>> DispatchBatchAsync(CancellationToken cancellationToken = default);
}

namespace Application.BuildingBlocks.Messaging.Inbox;

/// <summary>Runs a consumer handler at most once per message id and consumer.</summary>
/// <remarks>
/// <para>
/// The inbox row is written only after the handler returns a successful <see cref="Result"/>, and
/// it is written in the same transaction as the handler's own writes, so the processed marker and
/// the effects it guards commit or roll back together.
/// </para>
/// <para>
/// A message id already recorded for the same consumer returns success without invoking the
/// handler, which makes redelivery a no-op.
/// </para>
/// <para>
/// A failed handler result is never recorded as processed; the failure is returned unchanged and
/// the message stays eligible for redelivery.
/// </para>
/// </remarks>
public interface IInboxStore
{
    Task<Result> ExecuteOnceAsync(
        Guid messageId,
        string consumer,
        Func<CancellationToken, Task<Result>> handler,
        CancellationToken cancellationToken = default);
}

namespace Application.Common.Messaging.Inbox;

/// <summary>Runs a consumer handler at most once per message id and consumer.</summary>
public interface IInboxStore
{
    Task<Result> ExecuteOnceAsync(
        Guid messageId,
        string consumer,
        Func<CancellationToken, Task<Result>> handler,
        CancellationToken cancellationToken = default);
}

using Application.Common.Caching;
using Application.Common.Messaging.Handlers;
using Application.Common.Messaging.Inbox;
using Application.Features.Todos.Events;

namespace Infrastructure.Integrations.Messaging.Handlers;

/// <summary>Invalidates the cached todo after the completion event is consumed.</summary>
public sealed class TodoCompletedIntegrationHandler(
    ICacheService cache,
    IInboxStore inbox)
    : IntegrationEventHandler<TodoCompletedIntegration>(inbox)
{
    /// <summary>Wolverine entry point.</summary>
    /// <param name="message">The integration event.</param>
    /// <param name="cancellationToken">A token that cancels the handler.</param>
    /// <returns>A task that completes when the handler finishes.</returns>
    public Task Handle(
        TodoCompletedIntegration message,
        CancellationToken cancellationToken)
        => ExecuteOnceAsync(message, cancellationToken);

    /// <inheritdoc />
    protected override async Task<Result> HandleAsync(
        TodoCompletedIntegration message,
        CancellationToken cancellationToken)
    {
        // Cache: the read model may serve the stale completion until this entry is evicted.
        return await cache
            .RemoveAsync($"todo:{message.TodoId}", cancellationToken)
            .ConfigureAwait(false);
    }
}
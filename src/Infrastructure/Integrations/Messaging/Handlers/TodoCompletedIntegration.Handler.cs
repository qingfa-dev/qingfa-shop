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
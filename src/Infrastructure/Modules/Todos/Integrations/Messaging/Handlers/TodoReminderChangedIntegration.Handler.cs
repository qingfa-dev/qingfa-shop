using Application.BuildingBlocks.Caching;
using Application.BuildingBlocks.Messaging.Handlers;
using Application.BuildingBlocks.Messaging.Inbox;
using Application.Modules.Todos.Events;

namespace Infrastructure.Modules.Todos.Integrations.Messaging.Handlers;

/// <summary>Invalidates cached reminder and todo entries after a reminder changes.</summary>
public sealed class TodoReminderChangedIntegrationHandler(
    ICacheService cache,
    IInboxStore inbox)
    : IntegrationEventHandler<TodoReminderChangedIntegration>(inbox)
{
    /// <summary>Wolverine entry point.</summary>
    /// <param name="message">The integration event.</param>
    /// <param name="cancellationToken">A token that cancels the handler.</param>
    /// <returns>A task that completes when the handler finishes.</returns>
    public Task Handle(
        TodoReminderChangedIntegration message,
        CancellationToken cancellationToken)
        => ExecuteOnceAsync(message, cancellationToken);

    /// <inheritdoc />
    protected override async Task<Result> HandleAsync(
        TodoReminderChangedIntegration message,
        CancellationToken cancellationToken)
    {
        // Cache: both the reminder projection and its todo may serve stale state until evicted.
        Result reminder = await cache
            .RemoveAsync($"reminder:{message.ReminderId}", cancellationToken)
            .ConfigureAwait(false);

        if (reminder.IsFailure)
            return reminder;

        return await cache
            .RemoveAsync($"todo:{message.TodoId}", cancellationToken)
            .ConfigureAwait(false);
    }
}

using Application.Common.Messaging.Bus;

using Core.Common.Monad.Metadata;
using Core.Todos.Items;

using MediatR;

namespace Application.Features.Todos.Events;

/// <summary>Bridges completed status changes to the integration bus.</summary>
public sealed class TodoStatusChangedIntegrationHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<TodoStatusChanged>
{
    /// <inheritdoc />
    public async Task Handle(
        TodoStatusChanged notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // Skip: only completion has an external contract today.
        if (notification.To != TodoStatus.Completed)
            return;

        var integration = new TodoCompletedIntegration
        {
            TodoId = notification.TodoId,
            TenantId = notification.TenantId,
            CompletedAtUtc = notification.OccurredAtUtc,
            Metadata = new DefaultMetadata(notification.Metadata),
        };

        integration.SetTenantId(notification.TenantId);

        Result result = await publisher
            .PublishAsync(integration, cancellationToken)
            .ConfigureAwait(false);

        result.ThrowIfFailure();
    }
}

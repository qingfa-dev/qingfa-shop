using Application.Common.Messaging.Bus;

using Core.Common.Monad.Metadata;

using Core.Todos.Items;

using MediatR;

namespace Application.Features.Todos.Events;

/// <summary>Bridges the <see cref="TodoCompleted"/> domain event to the integration bus.</summary>
public sealed class TodoCompletedIntegrationHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<TodoCompleted>
{
    /// <inheritdoc />
    public async Task Handle(
        TodoCompleted notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var integration = new TodoCompletedIntegration
        {
            TodoId = notification.TodoId,
            TenantId = notification.TenantId,
            CompletedAtUtc = notification.CompletedAtUtc,
            Metadata = new DefaultMetadata(notification.Metadata),
        };

        Result result = await publisher
            .PublishAsync(integration, cancellationToken)
            .ConfigureAwait(false);

        result.ThrowIfFailure();
    }
}
using Application.BuildingBlocks.Messaging.Bus;

using Core.BuildingBlocks.Modeling.Metadata;

using Core.Modules.Todos.Reminders;

using MediatR;

namespace Application.Modules.Todos.Events;

/// <summary>Bridges the <see cref="ReminderDismissed"/> domain event to the integration bus.</summary>
public sealed class ReminderDismissedIntegrationHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<ReminderDismissed>
{
    /// <inheritdoc />
    public async Task Handle(
        ReminderDismissed notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var integration = new TodoReminderChangedIntegration
        {
            ReminderId = notification.ReminderId,
            TodoId = notification.TodoId,
            TenantId = notification.TenantId,
            Change = TodoReminderChange.Dismissed,
            RemindAtUtc = notification.RemindAtUtc,
            DismissedAtUtc = notification.DismissedAtUtc,
            Metadata = new DefaultMetadata(notification.Metadata),
        };

        // Enforce: the outbox envelope reads tenant from metadata, so the contract field must win
        // even when the source event carries no tenant envelope.
        integration.SetTenantId(notification.TenantId);

        Result result = await publisher
            .PublishAsync(integration, cancellationToken)
            .ConfigureAwait(false);

        result.ThrowIfFailure();
    }
}

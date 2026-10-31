using Application.BuildingBlocks.Messaging.Bus;
using Application.Modules.Payments.Events;

using Core.BuildingBlocks.Modeling.Metadata;
using Core.Modules.Payments.Refunds;

using MediatR;

namespace Application.Modules.Payments.Integrations;

/// <summary>Bridges the <see cref="RefundSucceeded"/> domain event to the integration bus.</summary>
public sealed class RefundSucceededIntegrationHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<RefundSucceeded>
{
    /// <inheritdoc />
    public async Task Handle(RefundSucceeded notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var integration = new PaymentRefundedIntegration
        {
            PaymentId = notification.PaymentId,
            RefundId = notification.RefundId,
            Amount = notification.Amount.Amount,
            Currency = notification.Amount.Currency.Code ?? string.Empty,
            RefundedAtUtc = notification.OccurredAtUtc,
            Metadata = new DefaultMetadata(notification.Metadata)
        };

        Result result = await publisher
            .PublishAsync(integration, cancellationToken)
            .ConfigureAwait(false);

        result.ThrowIfFailure();
    }
}

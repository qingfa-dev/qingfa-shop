using Application.BuildingBlocks.Messaging.Bus;
using Application.Modules.Payments.Events;

using Core.BuildingBlocks.Modeling.Metadata;
using Core.Modules.Payments.Intents;

using MediatR;

namespace Application.Modules.Payments.Integrations;

/// <summary>Bridges the <see cref="PaymentFailed"/> domain event to the integration bus.</summary>
public sealed class PaymentFailedIntegrationHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<PaymentFailed>
{
    /// <inheritdoc />
    public async Task Handle(PaymentFailed notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var integration = new PaymentFailedIntegration
        {
            PaymentId = notification.PaymentId,
            FailureCode = notification.FailureCode,
            FailedAtUtc = notification.OccurredAtUtc,
            Metadata = new DefaultMetadata(notification.Metadata)
        };

        Result result = await publisher
            .PublishAsync(integration, cancellationToken)
            .ConfigureAwait(false);

        result.ThrowIfFailure();
    }
}

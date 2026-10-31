using Application.BuildingBlocks.Messaging.Bus;
using Application.Modules.Payments.Events;

using Core.BuildingBlocks.Modeling.Metadata;
using Core.Modules.Payments.Intents;

using MediatR;

namespace Application.Modules.Payments.Integrations;

/// <summary>Bridges the <see cref="PaymentCaptured"/> domain event to the integration bus.</summary>
/// <remarks>
/// The envelope carries no card data or customer identity: consumers receive the payment id, amount,
/// currency, and the caller's own external reference.
/// </remarks>
public sealed class PaymentCapturedIntegrationHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<PaymentCaptured>
{
    /// <inheritdoc />
    public async Task Handle(PaymentCaptured notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var integration = new PaymentCapturedIntegration
        {
            PaymentId = notification.PaymentId,
            Amount = notification.TotalCapturedAmount.Amount,
            Currency = notification.TotalCapturedAmount.Currency.Code ?? string.Empty,
            CapturedAtUtc = notification.OccurredAtUtc,
            Metadata = new DefaultMetadata(notification.Metadata)
        };

        Result result = await publisher
            .PublishAsync(integration, cancellationToken)
            .ConfigureAwait(false);

        result.ThrowIfFailure();
    }
}

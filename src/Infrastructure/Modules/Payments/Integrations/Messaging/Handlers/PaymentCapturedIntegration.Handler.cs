using Application.BuildingBlocks.Messaging.Handlers;
using Application.BuildingBlocks.Messaging.Inbox;
using Application.Modules.Payments.Events;

using MediatR;

namespace Infrastructure.Modules.Payments.Integrations.Messaging.Handlers;

/// <summary>Consumes settled payments so notification and reporting can react.</summary>
/// <remarks>
/// The handler is deliberately a seam: the inbox guard makes consumption idempotent, and the
/// notification port is where a mail or webhook consumer plugs in later.
/// </remarks>
public sealed class PaymentCapturedIntegrationHandler(
    IInboxStore inbox,
    IPublisher publisher)
    : IntegrationEventHandler<PaymentCapturedIntegration>(inbox)
{
    /// <summary>Wolverine entry point.</summary>
    /// <param name="message">The integration event.</param>
    /// <param name="cancellationToken">A token that cancels the handler.</param>
    /// <returns>A task that completes when the handler finishes.</returns>
    public Task Handle(
        PaymentCapturedIntegration message,
        CancellationToken cancellationToken)
        => ExecuteOnceAsync(message, cancellationToken);

    /// <inheritdoc />
    protected override Task<Result> HandleAsync(
        PaymentCapturedIntegration message,
        CancellationToken cancellationToken)
    {
        _ = publisher;
        _ = message;

        // Handle: nothing reacts yet; the inbox row still records the delivery exactly once.
        return Task.FromResult(Result.Ok());
    }
}

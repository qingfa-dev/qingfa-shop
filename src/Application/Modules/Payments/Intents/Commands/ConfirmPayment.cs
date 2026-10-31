using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Intents;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using MediatR;

namespace Application.Modules.Payments.Intents.Commands;

/// <summary>Refreshes a payment after the customer returns from the provider.</summary>
public static class ConfirmPayment
{
    #region UseCase

    /// <summary>The command.</summary>
    /// <param name="PaymentId">The payment to refresh.</param>
    public sealed record UseCase(Guid PaymentId) : ICommand<PaymentResponseModel>, INonTransactionalCommand;

    #endregion


    #region Handler

    /// <summary>Handles the command.</summary>
    public sealed class UseCaseHandler(
        IPaymentReadStore store,
        IEventSourcedRepository<Payment, Guid> repository,
        IPaymentGatewayRegistry registry,
        IMediator mediator) : ICommandHandler<UseCase, PaymentResponseModel>
    {
        /// <inheritdoc />
        public async Task<Result<PaymentResponseModel>> Handle(UseCase command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            return await ResolveAsync(command, cancellationToken)
                .BindAsync((target, ct) => RefreshAsync(target, ct), cancellationToken);
        }

        /// <summary>Resolves the provider and reference from the projection or the event stream.</summary>
        private async Task<Result<Target>> ResolveAsync(UseCase command, CancellationToken cancellationToken)
        {
            Result<PaymentReadModel> row = await store.FindAsync(command.PaymentId, cancellationToken);

            if (row.IsSuccess)
                return Result<Target>.Success(new Target(
                    PaymentProviderExtension.FromWire(row.Value.Provider),
                    row.Value.ProviderReference,
                    PaymentStatusExtension.FromWire(row.Value.Status)));

            // Fallback: read the event stream while the projection is still catching up.
            Result<Payment> loaded = await repository.LoadAsync(command.PaymentId, cancellationToken);

            return loaded.Map(payment => new Target(
                payment.Provider,
                payment.ProviderReference,
                payment.Status));
        }

        /// <summary>Asks the provider for the current state and settles through the shared command.</summary>
        private async Task<Result<PaymentResponseModel>> RefreshAsync(
            Target target,
            CancellationToken cancellationToken)
        {
            if (target.ProviderReference is null)
                return Result<PaymentResponseModel>.Fail(
                    PaymentsOutcome.Failure.InvalidState(target.Status, "confirmed"));

            return await registry
                .Resolve(target.Provider)
                .BindAsync(
                    (gateway, ct) => PaymentMetrics.TrackAsync(
                        target.Provider,
                        PaymentConstant.Operations.Retrieve,
                        () => gateway.RetrieveAsync(target.ProviderReference, ct)),
                    cancellationToken)
                .BindAsync(
                    (intent, ct) => mediator.Send(
                        new SettlePayment.UseCase(
                            target.Provider,
                            target.ProviderReference,
                            ToEventType(intent.Status),
                            null,
                            intent.FailureCode,
                            $"{PaymentConstant.IdempotencyPrefixes.Confirm}{target.ProviderReference}"),
                        ct),
                    cancellationToken);
        }

        /// <summary>Maps a provider intent status onto the observed event type.</summary>
        private static PaymentProviderEventType ToEventType(PaymentProviderIntentStatus status) => status switch
        {
            PaymentProviderIntentStatus.Captured => PaymentProviderEventType.Captured,
            PaymentProviderIntentStatus.Authorized => PaymentProviderEventType.Authorized,
            PaymentProviderIntentStatus.Cancelled => PaymentProviderEventType.Cancelled,
            _ => PaymentProviderEventType.Failed
        };
    }

    #region Models

    /// <summary>The provider identity a confirmation needs.</summary>
    private sealed record Target(PaymentProvider Provider, string? ProviderReference, PaymentStatus Status);

    #endregion

    #endregion
}

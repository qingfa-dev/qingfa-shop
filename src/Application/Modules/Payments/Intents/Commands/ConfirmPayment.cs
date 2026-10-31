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

            Result<PaymentReadModel> row = await store.FindAsync(command.PaymentId, cancellationToken);

            PaymentProvider provider;
            string? providerReference;
            PaymentStatus status;

            if (row is null)            {
                // Fallback: read the event stream while the projection is still catching up.
                Result<Payment> loaded = await repository.LoadAsync(command.PaymentId, cancellationToken);

                if (loaded.IsFailure)
                    return Result<PaymentResponseModel>.Fail(
                        PaymentsOutcome.Failure.NotFound(command.PaymentId));

                provider = loaded.Value.Provider;
                providerReference = loaded.Value.ProviderReference;
                status = loaded.Value.Status;
            }
            else
            {
                provider = PaymentProviderExtension.FromWire(row.Value.Provider);
                providerReference = row.Value.ProviderReference;
                status = PaymentStatusExtension.FromWire(row.Value.Status);
            }

            if (providerReference is null)
                return Result<PaymentResponseModel>.Fail(
                    PaymentsOutcome.Failure.InvalidState(status, "confirm"));

            Result<IPaymentGateway> resolved = registry.Resolve(provider);

            if (resolved.IsFailure)
                return Result<PaymentResponseModel>.Fail(resolved.Errors);

            Result<PaymentIntentResult> intent = await PaymentMetrics.TrackAsync(
                provider,
                PaymentConstant.Operations.Retrieve,
                () => resolved.Value.RetrieveAsync(providerReference, cancellationToken));

            if (intent.IsFailure)
                return Result<PaymentResponseModel>.Fail(intent.Errors);

            PaymentProviderEventType observed = intent.Value.Status switch
            {
                PaymentProviderIntentStatus.Captured => PaymentProviderEventType.Captured,
                PaymentProviderIntentStatus.Authorized => PaymentProviderEventType.Authorized,
                PaymentProviderIntentStatus.Cancelled => PaymentProviderEventType.Cancelled,
                _ => PaymentProviderEventType.Failed
            };

            return await mediator.Send(
                new SettlePayment.UseCase(
                    provider,
                    providerReference,
                    observed,
                    null,
                    intent.Value.FailureCode,
                    $"{PaymentConstant.IdempotencyPrefixes.Confirm}{command.PaymentId:N}"),
                cancellationToken);
        }
    }

    #endregion
}

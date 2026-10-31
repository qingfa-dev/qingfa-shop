using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Intents;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Ports.Models;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

namespace Application.Modules.Payments.Intents.Commands;

/// <summary>Applies an observed provider state; shared by confirm and webhooks.</summary>
public static class SettlePayment
{
    #region UseCase

    /// <summary>The observed settlement.</summary>
    /// <param name="Provider">The provider that reported it.</param>
    /// <param name="ProviderReference">The provider reference.</param>
    /// <param name="Observed">The observed event type.</param>
    /// <param name="Amount">The observed amount, when the provider reports one.</param>
    /// <param name="FailureCode">The provider failure code, when failed.</param>
    /// <param name="ProviderEventId">The provider event id, used for correlation.</param>
    public sealed record UseCase(
        PaymentProvider Provider,
        string ProviderReference,
        PaymentProviderEventType Observed,
        Money? Amount,
        string? FailureCode,
        string ProviderEventId) : ICommand<PaymentResponseModel>;

    #endregion


    #region Handler

    /// <summary>Handles the command.</summary>
    public sealed class UseCaseHandler(
        IEventSourcedRepository<Payment, Guid> repository,
        IPaymentReferenceStore references,
        IPaymentReadStore store,
        TimeProvider clock) : ICommandHandler<UseCase, PaymentResponseModel>
    {
        /// <inheritdoc />
        public async Task<Result<PaymentResponseModel>> Handle(UseCase command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            // Validate: the provider identity is what links the observation to a payment.
            if (string.IsNullOrWhiteSpace(command.ProviderReference))
                return Result<PaymentResponseModel>.Fail(
                    PaymentsOutcome.Failure.ReferenceRequired("provider reference"));

            Guid? paymentId = await references.FindAsync(
                command.Provider, command.ProviderReference, cancellationToken);

            if (paymentId is null)
            {
                PaymentReadModel? row = await store.FindByProviderReferenceAsync(
                    command.Provider, command.ProviderReference, cancellationToken);

                paymentId = row?.Id;
            }

            if (paymentId is null)
                return Result<PaymentResponseModel>.Fail(
                    PaymentsOutcome.Failure.ReferenceNotFound(command.ProviderReference));

            Result<Payment> loaded = await repository.LoadAsync(paymentId.Value, cancellationToken);

            if (loaded.IsFailure)
                return Result<PaymentResponseModel>.Fail(loaded.Errors);

            Payment payment = loaded.Value;

            // Handle: refund events are applied by the refund slices, never here.
            if (command.Observed is PaymentProviderEventType.Refunded or PaymentProviderEventType.RefundFailed)
                return Result<PaymentResponseModel>.Success(payment.ToResponse());

            PaymentObservation observation = command.Observed switch
            {
                PaymentProviderEventType.Authorized => PaymentObservation.Authorized,
                PaymentProviderEventType.Captured => PaymentObservation.Captured,
                PaymentProviderEventType.Failed => PaymentObservation.Failed,
                PaymentProviderEventType.Cancelled => PaymentObservation.Cancelled,
                PaymentProviderEventType.Expired => PaymentObservation.Expired,
                _ => PaymentObservation.Captured
            };

            Result<Payment> transition = payment.ApplyObservation(
                observation,
                command.ProviderReference,
                command.Amount,
                command.FailureCode,
                clock.GetUtcNow());

            if (transition.IsFailure)
            {
                Error error = transition.Errors[0];

                // Handle: a repeated terminal observation is idempotent, not an error.
                bool alreadyApplied = error.Code == PaymentsOutcome.Codes.InvalidState
                    && payment.Status is PaymentStatus.Captured
                        or PaymentStatus.Refunded
                        or PaymentStatus.PartiallyRefunded;

                return alreadyApplied
                    ? Result<PaymentResponseModel>.Success(payment.ToResponse())
                    : Result<PaymentResponseModel>.Fail(error);
            }

            Result saved = await repository.SaveAsync(transition.Value, cancellationToken);

            if (saved.IsFailure)
            {
                PaymentMetrics.Failed.Add(1);
                return Result<PaymentResponseModel>.Fail(saved.Errors);
            }

            PaymentMetrics.Settled.Add(1);
            return Result<PaymentResponseModel>.Success(transition.Value.ToResponse());
        }
    }

    #endregion
}

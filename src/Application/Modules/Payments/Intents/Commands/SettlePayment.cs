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
    /// <param name="CaptureReference">The provider capture id, when the provider reports one.</param>
    public sealed record UseCase(
        PaymentProvider Provider,
        string ProviderReference,
        PaymentProviderEventType Observed,
        Money? Amount,
        string? FailureCode,
        string ProviderEventId,
        string? CaptureReference = null) : ICommand<PaymentResponseModel>;

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

            return await ResolvePaymentIdAsync(command, cancellationToken)
                .BindAsync((id, ct) => repository.LoadAsync(id, ct), cancellationToken)
                .BindAsync((payment, ct) => ApplyAsync(payment, command, ct), cancellationToken);
        }

        /// <summary>Finds the payment id for the observed provider reference.</summary>
        private async Task<Result<Guid>> ResolvePaymentIdAsync(
            UseCase command,
            CancellationToken cancellationToken)
        {
            Result<Guid> reference = await references.FindAsync(
                command.Provider, command.ProviderReference, cancellationToken);

            if (reference.IsSuccess)
                return reference;

            Result<PaymentReadModel> row = await store.FindByProviderReferenceAsync(
                command.Provider, command.ProviderReference, cancellationToken);

            return row.Map(found => found.Id);
        }

        /// <summary>Applies the observed state and persists the transition.</summary>
        private async Task<Result<PaymentResponseModel>> ApplyAsync(
            Payment payment,
            UseCase command,
            CancellationToken cancellationToken)
        {
            // Handle: refund events are applied by the refund slices, never here.
            if (command.Observed is PaymentProviderEventType.Refunded or PaymentProviderEventType.RefundFailed)
                return Result<PaymentResponseModel>.Success(payment.ToResponse());

            Result<(Payment Payment, bool ShouldSave)> evaluated = Evaluate(
                payment.ApplyObservation(
                    ToObservation(command.Observed),
                    command.ProviderReference,
                    command.Amount,
                    command.FailureCode,
                    clock.GetUtcNow(),
                    command.CaptureReference),
                payment);

            return await evaluated
                .BindAsync(
                    (state, ct) => state.ShouldSave
                        ? SaveAsync(state.Payment, ct)
                        : Task.FromResult(Result<PaymentResponseModel>.Success(state.Payment.ToResponse())),
                    cancellationToken);
        }

        /// <summary>Decides whether the observation persisted a transition or was already applied.</summary>
        private static Result<(Payment Payment, bool ShouldSave)> Evaluate(
            Result<Payment> observation,
            Payment current)
        {
            if (!observation.IsFailure)
                return Result<(Payment Payment, bool ShouldSave)>.Success((observation.Value, true));

            // Handle: a repeated terminal observation is idempotent, not an error.
            bool alreadyApplied = observation.Errors[0].Code == PaymentsOutcome.Codes.InvalidState
                && current.Status is PaymentStatus.Captured
                    or PaymentStatus.Refunded
                    or PaymentStatus.PartiallyRefunded;

            return alreadyApplied
                ? Result<(Payment Payment, bool ShouldSave)>.Success((current, false))
                : Result<(Payment Payment, bool ShouldSave)>.Fail(observation.Errors);
        }

        /// <summary>Persists the transition and records the settlement metric.</summary>
        private async Task<Result<PaymentResponseModel>> SaveAsync(
            Payment payment,
            CancellationToken cancellationToken)
        {
            Result saved = await repository.SaveAsync(payment, cancellationToken);

            // Handle: record the settlement outcome once, then project the response.
            saved.Switch(RecordSettled, RecordFailed);

            return saved.Map(() => payment.ToResponse());
        }

        /// <summary>Maps a provider event type onto the aggregate observation.</summary>
        private static PaymentObservation ToObservation(PaymentProviderEventType observed) => observed switch
        {
            PaymentProviderEventType.Authorized => PaymentObservation.Authorized,
            PaymentProviderEventType.Failed => PaymentObservation.Failed,
            PaymentProviderEventType.Cancelled => PaymentObservation.Cancelled,
            PaymentProviderEventType.Expired => PaymentObservation.Expired,
            _ => PaymentObservation.Captured
        };

        /// <summary>Records one settled payment.</summary>
        private static void RecordSettled() => PaymentMetrics.Settled.Add(1);

        /// <summary>Records one failed persistence.</summary>
        private static void RecordFailed(List<Error> _) => PaymentMetrics.Failed.Add(1);
    }

    #endregion
}

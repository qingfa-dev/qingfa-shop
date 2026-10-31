using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

namespace Application.Modules.Payments.Intents.Commands;

/// <summary>Captures an authorized payment, fully or partially.</summary>
public static class CapturePayment
{
    #region Request

    /// <summary>The request body; a null amount captures the remainder.</summary>
    /// <param name="Amount">The amount to capture.</param>
    public sealed record Request(Money? Amount);

    #endregion

    #region UseCase

    /// <summary>The command.</summary>
    /// <param name="PaymentId">The payment to capture.</param>
    /// <param name="Amount">The amount to capture; null captures the remainder.</param>
    public sealed record UseCase(Guid PaymentId, Money? Amount) : ICommand<PaymentResponseModel>;

    #endregion


    #region Handler

    /// <summary>Handles the command.</summary>
    public sealed class UseCaseHandler(
        IEventSourcedRepository<Payment, Guid> repository,
        IPaymentGatewayRegistry registry,
        TimeProvider clock) : ICommandHandler<UseCase, PaymentResponseModel>
    {
        /// <inheritdoc />
        public async Task<Result<PaymentResponseModel>> Handle(UseCase command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            Result<Payment> captured = await repository
                .LoadAsync(command.PaymentId, cancellationToken)
                .BindAsync((payment, ct) => CaptureAsync(payment, command, ct), cancellationToken)
                .BindAsync((payment, ct) => SaveAsync(payment, ct), cancellationToken);

            return captured.Map(payment => payment.ToResponse());
        }

        /// <summary>Captures at the provider and records the transition on the aggregate.</summary>
        private async Task<Result<Payment>> CaptureAsync(
            Payment payment,
            UseCase command,
            CancellationToken cancellationToken)
        {
            // Validate: a payment without a provider reference cannot be captured.
            if (payment.ProviderReference is null)
                return Result<Payment>.Fail(PaymentsOutcome.Failure.ProviderReferenceMissing());

            Money amount = command.Amount
                ?? new Money(payment.RemainingCapturable, payment.Amount.Currency);

            return await registry.Resolve(payment.Provider)
                .BindAsync(
                    (gateway, ct) => CaptureAtProviderAsync(payment, gateway, amount, ct),
                    cancellationToken);
        }

        /// <summary>Calls the provider and maps the result onto the aggregate.</summary>
        private async Task<Result<Payment>> CaptureAtProviderAsync(
            Payment payment,
            IPaymentGateway gateway,
            Money amount,
            CancellationToken cancellationToken)
        {
            Result<PaymentCaptureResult> captured = await PaymentMetrics.TrackAsync(
                payment.Provider,
                PaymentConstant.Operations.Capture,
                () => gateway.CaptureAsync(
                    new PaymentCaptureRequest(
                        payment.ProviderReference!,
                        amount,
                        $"{PaymentConstant.IdempotencyPrefixes.Capture}{payment.Id:N}:{payment.CapturedAmount.Amount:0.####}"),
                    cancellationToken));

            return await captured
                .BindAsync((result, ct) => ApplyAsync(payment, result, amount, ct), cancellationToken);
        }

        /// <summary>Records a declined capture or the successful transition.</summary>
        private async Task<Result<Payment>> ApplyAsync(
            Payment payment,
            PaymentCaptureResult result,
            Money amount,
            CancellationToken cancellationToken)
        {
            if (result.Succeeded)
                return payment.Capture(amount, result.ProviderReference, clock.GetUtcNow());

            Result<Payment> failed = payment.CaptureFailed(
                amount,
                result.FailureCode ?? PaymentConstant.Reasons.CaptureFailed,
                null,
                clock.GetUtcNow());

            return await failed
                .BindAsync((recorded, ct) => SaveAsync(recorded, ct), cancellationToken)
                .BindAsync(
                    (_, _) => Task.FromResult(Result<Payment>.Fail(
                        PaymentsOutcome.Failure.ProviderDeclined(result.FailureCode))),
                    cancellationToken);
        }

        /// <summary>Persists the aggregate and reports the settlement metric.</summary>
        private async Task<Result<Payment>> SaveAsync(Payment payment, CancellationToken cancellationToken)
        {
            Result saved = await repository.SaveAsync(payment, cancellationToken);

            saved.Switch(
                RecordSettled,
                _ => PaymentMetrics.Failed.Add(1));

            return saved.Map(() => payment);
        }

        /// <summary>Records one settled capture.</summary>
        private static void RecordSettled() => PaymentMetrics.Settled.Add(1);
    }

    #endregion
}

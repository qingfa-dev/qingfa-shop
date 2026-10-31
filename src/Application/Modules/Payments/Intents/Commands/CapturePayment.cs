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

            Result<Payment> loaded = await repository.LoadAsync(command.PaymentId, cancellationToken);

            if (loaded.IsFailure)
                return Result<PaymentResponseModel>.Fail(loaded.Errors);

            Payment payment = loaded.Value;

            if (payment.ProviderReference is null)
                return Result<PaymentResponseModel>.Fail(PaymentsOutcome.Failure.ProviderReferenceMissing());

            Money amount = command.Amount
                ?? new Money(payment.RemainingCapturable, payment.Amount.Currency);

            Result<IPaymentGateway> resolved = registry.Resolve(payment.Provider);

            if (resolved.IsFailure)
                return Result<PaymentResponseModel>.Fail(resolved.Errors);

            Result<PaymentCaptureResult> captured = await resolved.Value.CaptureAsync(
                new PaymentCaptureRequest(
                    payment.ProviderReference,
                    amount,
                    $"{PaymentConstant.IdempotencyPrefixes.Capture}{payment.Id:N}:{payment.CapturedAmount.Amount:0.####}"),
                cancellationToken);

            if (captured.IsFailure)
                return Result<PaymentResponseModel>.Fail(captured.Errors);

            if (!captured.Value.Succeeded)
            {
                Result<Payment> failed = payment.CaptureFailed(
                    amount,
                    captured.Value.FailureCode ?? PaymentConstant.Reasons.CaptureFailed,
                    null,
                    clock.GetUtcNow());

                if (failed.IsFailure)
                    return Result<PaymentResponseModel>.Fail(failed.Errors);

                await repository.SaveAsync(failed.Value, cancellationToken);

                return Result<PaymentResponseModel>.Fail(
                    PaymentsOutcome.Failure.ProviderDeclined(captured.Value.FailureCode));
            }

            Result<Payment> transition = payment.Capture(
                amount, captured.Value.ProviderReference, clock.GetUtcNow());

            if (transition.IsFailure)
                return Result<PaymentResponseModel>.Fail(transition.Errors);

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

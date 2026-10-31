using Core.BuildingBlocks.Domain.ValueObjects.Money;

namespace Core.Modules.Payments.Intents;

/// <summary>Validates the payment invariants that do not depend on provider I/O.</summary>
/// <remarks>
/// The aggregate calls these before it records anything, so a rejected mutation never appends an
/// event. Every failure is a <see cref="PaymentsOutcome"/> error; the module has no other source
/// of validation errors.
/// </remarks>
public static class PaymentValidator
{
    /// <summary>Validates a newly initiated payment.</summary>
    /// <param name="payment">Candidate payment.</param>
    /// <param name="maxAmount">The configured ceiling for a single payment.</param>
    /// <returns>Success, or the first broken invariant.</returns>
    public static Result ValidateInitiation(Payment payment, decimal maxAmount)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return Result.Ok()
            .Ensure(() => payment.Amount.Amount > 0,
                PaymentsOutcome.Failure.AmountInvalid("the amount must be greater than zero"))
            .Ensure(() => payment.Amount.Amount <= maxAmount,
                PaymentsOutcome.Failure.AmountExceedsLimit(payment.Amount.Amount, maxAmount))
            .Ensure(() => !string.IsNullOrWhiteSpace(payment.ExternalReference),
                PaymentsOutcome.Failure.ReferenceRequired("external reference"))
            .Ensure(() => payment.ExternalReference.Length <= PaymentConstant.Constraints.ReferenceMaxLength,
                PaymentsOutcome.Failure.ReferenceTooLong(
                    "external reference", PaymentConstant.Constraints.ReferenceMaxLength))
            .Ensure(() => !string.IsNullOrWhiteSpace(payment.IdempotencyKey),
                PaymentsOutcome.Failure.IdempotencyKeyRequired())
            .Ensure(() => payment.IdempotencyKey.Length <= PaymentConstant.Constraints.IdempotencyKeyMaxLength,
                PaymentsOutcome.Failure.IdempotencyKeyTooLong())
            .Ensure(() => payment.ProviderReference is null
                    || payment.ProviderReference.Length <= PaymentConstant.Constraints.ProviderReferenceMaxLength,
                PaymentsOutcome.Failure.ReferenceTooLong(
                    "provider reference", PaymentConstant.Constraints.ProviderReferenceMaxLength));
    }

    /// <summary>Validates a capture against the payment it targets.</summary>
    /// <param name="payment">The payment being captured.</param>
    /// <param name="captured">The amount to capture.</param>
    /// <param name="providerReference">The provider reference reported by the capture.</param>
    /// <returns>Success, or the first broken invariant.</returns>
    public static Result ValidateCapture(Payment payment, Money captured, string? providerReference)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return Result.Ok()
            .Ensure(() => !string.IsNullOrWhiteSpace(providerReference),
                PaymentsOutcome.Failure.ProviderReferenceMissing())
            .Ensure(() => providerReference!.Length <= PaymentConstant.Constraints.ProviderReferenceMaxLength,
                PaymentsOutcome.Failure.ReferenceTooLong(
                    "provider reference", PaymentConstant.Constraints.ProviderReferenceMaxLength))
            .Ensure(() => captured.Amount > 0,
                PaymentsOutcome.Failure.AmountInvalid("the captured amount must be positive"))
            .Ensure(() => captured.Currency == payment.Amount.Currency,
                PaymentsOutcome.Failure.CurrencyMismatch(
                    payment.Amount.Currency.Code ?? string.Empty,
                    captured.Currency.Code ?? string.Empty))
            .Ensure(() => captured.Amount <= payment.RemainingCapturable,
                PaymentsOutcome.Failure.AmountInvalid("the captured amount exceeds the remaining amount"));
    }

    /// <summary>Validates the refund reason on its own.</summary>
    /// <param name="reason">Why the funds are returned.</param>
    /// <returns>Success, or the reason failure.</returns>
    public static Result ValidateRefundReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Fail(PaymentsOutcome.Failure.RefundReasonRequired());

        return reason.Length <= PaymentConstant.Constraints.ReasonMaxLength
            ? Result.Ok()
            : Result.Fail(PaymentsOutcome.Failure.RefundReasonRequired());
    }

    /// <summary>Validates a refund against the payment it targets.</summary>
    /// <param name="payment">The payment being refunded.</param>
    /// <param name="amount">The amount to refund.</param>
    /// <param name="reason">Why the funds are returned.</param>
    /// <returns>Success, or the first broken invariant.</returns>
    public static Result ValidateRefund(Payment payment, Money amount, string? reason)
    {
        ArgumentNullException.ThrowIfNull(payment);

        Result reasonCheck = ValidateRefundReason(reason);

        if (reasonCheck.IsFailure)
            return reasonCheck;

        return Result.Ok()
            .Ensure(() => amount.Amount > 0,
                PaymentsOutcome.Failure.AmountInvalid("the refund amount must be positive"))
            .Ensure(() => amount.Currency == payment.Amount.Currency,
                PaymentsOutcome.Failure.CurrencyMismatch(
                    payment.Amount.Currency.Code ?? string.Empty,
                    amount.Currency.Code ?? string.Empty))
            .Ensure(() => amount.Amount <= payment.CreditAllowed,
                PaymentsOutcome.Failure.RefundExceedsCredit(amount.Amount, payment.CreditAllowed));
    }

    /// <summary>Validates a dispute against the payment it targets.</summary>
    /// <param name="payment">The disputed payment.</param>
    /// <param name="amount">The disputed amount.</param>
    /// <returns>Success, or the first broken invariant.</returns>
    public static Result ValidateDispute(Payment payment, Money amount)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return Result.Ok()
            .Ensure(() => amount.Amount > 0,
                PaymentsOutcome.Failure.AmountInvalid("the disputed amount must be positive"))
            .Ensure(() => amount.Currency == payment.Amount.Currency,
                PaymentsOutcome.Failure.CurrencyMismatch(
                    payment.Amount.Currency.Code ?? string.Empty,
                    amount.Currency.Code ?? string.Empty))
            .Ensure(() => amount.Amount <= payment.CapturedAmount.Amount,
                PaymentsOutcome.Failure.DisputeExceedsCaptured(
                    amount.Amount, payment.CapturedAmount.Amount));
    }

    /// <summary>Clamps a provider failure code to the persisted width.</summary>
    /// <param name="failureCode">The provider code, if any.</param>
    /// <returns>A code that fits the column, or <c>null</c>.</returns>
    public static string? NormalizeFailureCode(string? failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
            return null;

        return failureCode.Length <= PaymentConstant.Constraints.FailureCodeMaxLength
            ? failureCode
            : failureCode[..PaymentConstant.Constraints.FailureCodeMaxLength];
    }
}

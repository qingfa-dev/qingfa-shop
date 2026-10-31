namespace Core.Modules.Payments.Intents;

/// <summary>Validates payment invariants that do not depend on provider I/O.</summary>
public static class PaymentValidator
{
    /// <summary>Validates a newly initiated payment.</summary>
    /// <param name="payment">Candidate payment.</param>
    /// <returns>Success, or the first broken invariant.</returns>
    public static Result ValidateInitiation(Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return Result.Ok()
            .Ensure(() => payment.Amount.Amount > 0,
                PaymentsOutcome.Failure.AmountInvalid("must be greater than zero"))
            .Ensure(() => !string.IsNullOrWhiteSpace(payment.ExternalReference),
                PaymentsOutcome.Failure.ReferenceRequired("external reference"))
            .Ensure(() => payment.ExternalReference.Length <= PaymentConstant.Constraints.ReferenceMaxLength,
                PaymentsOutcome.Failure.ReferenceTooLong("external reference", PaymentConstant.Constraints.ReferenceMaxLength))
            .Ensure(() => !string.IsNullOrWhiteSpace(payment.IdempotencyKey),
                PaymentsOutcome.Failure.IdempotencyKeyRequired())
            .Ensure(() => payment.IdempotencyKey.Length <= PaymentConstant.Constraints.IdempotencyKeyMaxLength,
                PaymentsOutcome.Failure.IdempotencyKeyTooLong());
    }
}
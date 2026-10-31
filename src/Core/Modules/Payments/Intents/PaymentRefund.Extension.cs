using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments.Refunds;

namespace Core.Modules.Payments.Intents;

/// <summary>Refund transitions on the payment aggregate.</summary>
public static class PaymentRefundExtension
{
    /// <summary>Requests a refund and validates the captured remainder.</summary>
    public static Result<Payment> RequestRefund(
        this Payment payment,
        Guid refundId,
        Money amount,
        string reason,
        string idempotencyKey,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (payment.Status is not (PaymentStatus.Captured or PaymentStatus.PartiallyRefunded))
            return Result<Payment>.Fail(PaymentsOutcome.Failure.RefundNotCaptured(payment.Status));

        if (payment.Refunds.Any(refund => refund.Id == refundId))
            return Result<Payment>.Success(payment);

        if (amount.Amount <= 0 || amount.Currency != payment.Amount.Currency)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.AmountInvalid("refund amount is out of range"));

        if (amount.Amount > payment.CreditAllowed)
            return Result<Payment>.Fail(
                PaymentsOutcome.Failure.RefundExceedsCredit(amount.Amount, payment.CreditAllowed));

        return payment.RaiseEvent(new RefundRequested(
            refundId, payment.Id, amount, reason, idempotencyKey, nowUtc));
    }

    /// <summary>Confirms a refund and advances the payment totals.</summary>
    public static Result<Payment> CompleteRefund(
        this Payment payment,
        Guid refundId,
        string providerReference,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(payment);

        Refund? refund = payment.Refunds.FirstOrDefault(item => item.Id == refundId);
        if (refund is null)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.RefundNotFound(refundId));

        if (refund.Status is RefundStatus.Succeeded)
            return Result<Payment>.Success(payment);

        return payment.RaiseEvent(new RefundSucceeded(
            refundId, payment.Id, refund.Amount, providerReference, nowUtc));
    }

    /// <summary>Records a rejected refund.</summary>
    public static Result<Payment> FailRefund(
        this Payment payment,
        Guid refundId,
        string failureCode,
        string? failureMessage,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return payment.RaiseEvent(new RefundFailed(
            refundId, payment.Id, failureCode, failureMessage, nowUtc));
    }
}
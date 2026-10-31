using System.Security.Cryptography;
using System.Text;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments.Disputes;

namespace Core.Modules.Payments.Intents;

/// <summary>Dispute transitions on the payment aggregate.</summary>
/// <remarks>
/// Disputes are tracked beside the payment status: a lost dispute withdraws funds at the provider, but
/// the module keeps <see cref="Payment.CapturedAmount"/> and <see cref="Payment.RefundedAmount"/>
/// untouched so reconciliation against a refund cannot double count the movement.
/// </remarks>
public static class PaymentDisputeExtension
{
    /// <summary>Records an opened dispute.</summary>
    /// <param name="payment">The disputed payment.</param>
    /// <param name="disputeId">The local dispute id.</param>
    /// <param name="providerDisputeReference">The provider dispute reference.</param>
    /// <param name="amount">The disputed amount.</param>
    /// <param name="reason">The provider reason.</param>
    /// <param name="nowUtc">The current instant.</param>
    /// <returns>The payment, or a stable failure.</returns>
    public static Result<Payment> OpenDispute(
        this Payment payment,
        Guid disputeId,
        string providerDisputeReference,
        Money amount,
        string reason,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (payment.Status is not (PaymentStatus.Captured or PaymentStatus.PartiallyRefunded))
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(payment.Status, "disputed"));

        if (payment.Disputes.Any(dispute => dispute.Id == disputeId))
            return Result<Payment>.Success(payment);

        Result validation = PaymentValidator.ValidateDispute(payment, amount);

        if (validation.IsFailure)
            return Result<Payment>.Fail(validation.Errors);

        return payment.RaiseEvent(new DisputeOpened(
            payment.Id,
            disputeId,
            providerDisputeReference,
            amount,
            reason,
            nowUtc));
    }

    /// <summary>Resolves an open dispute by its provider reference.</summary>
    /// <param name="payment">The disputed payment.</param>
    /// <param name="providerDisputeReference">The provider dispute reference.</param>
    /// <param name="outcome">The provider outcome.</param>
    /// <param name="nowUtc">The current instant.</param>
    /// <returns>The payment, or a stable failure.</returns>
    public static Result<Payment> ResolveDispute(
        this Payment payment,
        string providerDisputeReference,
        DisputeOutcome outcome,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(payment);

        Dispute? dispute = payment.Disputes.FirstOrDefault(
            item => string.Equals(item.ProviderReference, providerDisputeReference, StringComparison.Ordinal));

        return dispute is null
            ? Result<Payment>.Fail(PaymentsOutcome.Failure.DisputeNotFound(DeterministicId(providerDisputeReference)))
            : payment.ResolveDispute(dispute.Id, outcome, nowUtc);
    }

    /// <summary>Derives a stable local dispute id from the provider reference.</summary>
    /// <param name="providerDisputeReference">The provider dispute reference.</param>
    /// <returns>A deterministic identifier, so a replayed webhook cannot open a second dispute.</returns>
    public static Guid DeterministicId(string providerDisputeReference)
    {
        ArgumentNullException.ThrowIfNull(providerDisputeReference);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(providerDisputeReference));

        return new Guid(hash.AsSpan(0, PaymentConstant.Defaults.DeterministicIdBytes));
    }

    /// <summary>Resolves an open dispute.</summary>
    /// <param name="payment">The disputed payment.</param>
    /// <param name="disputeId">The local dispute id.</param>
    /// <param name="outcome">The provider outcome.</param>
    /// <param name="nowUtc">The current instant.</param>
    /// <returns>The payment, or a stable failure.</returns>
    public static Result<Payment> ResolveDispute(
        this Payment payment,
        Guid disputeId,
        DisputeOutcome outcome,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(payment);

        Dispute? dispute = payment.Disputes.FirstOrDefault(item => item.Id == disputeId);

        if (dispute is null)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.DisputeNotFound(disputeId));

        if (!dispute.IsOpen)
            return Result<Payment>.Success(payment);

        return payment.RaiseEvent(new DisputeResolved(payment.Id, disputeId, outcome, nowUtc));
    }
}

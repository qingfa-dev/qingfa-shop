using Core.BuildingBlocks.Domain.EventSourcing;
using Core.BuildingBlocks.Domain.ValueObjects.Money;

namespace Core.Modules.Payments.Intents;

/// <summary>Creates payment aggregates.</summary>
public static class PaymentFactory
{
    /// <summary>Creates a pending payment and records <see cref="PaymentInitiated"/>.</summary>
    public static Result<Payment> Initiate(
        Guid id,
        PaymentProvider provider,
        string? providerReference,
        Money amount,
        PaymentCaptureMode captureMode,
        string? externalReference,
        string? idempotencyKey,
        string? idempotencyFingerprint,
        Guid customerId,
        string? approvalUrl,
        DateTimeOffset? expiresAt,
        DateTimeOffset nowUtc,
        decimal maxAmount = PaymentConstant.Defaults.MaxAmount)
    {
        var candidate = Payment.Draft(
            id,
            provider,
            providerReference,
            amount,
            captureMode,
            externalReference,
            idempotencyKey,
            idempotencyFingerprint,
            customerId,
            approvalUrl,
            expiresAt,
            nowUtc);

        return Result<Payment>.Success(candidate)
            .Ensure(payment => PaymentValidator.ValidateInitiation(payment, maxAmount))
            .Bind(payment => payment.RaiseEvent(new PaymentInitiated(
                payment.Id,
                payment.Provider,
                payment.ProviderReference,
                payment.Amount,
                payment.CaptureMode,
                payment.ExternalReference,
                payment.IdempotencyKey,
                payment.IdempotencyFingerprint,
                payment.CustomerId,
                payment.ApprovalUrl,
                payment.ExpiresAt,
                payment.CreatedAt)));
    }
}

/// <summary>Creates an empty payment for rehydration.</summary>
public sealed class PaymentAggregateFactory : IAggregateFactory<Payment>
{
    /// <inheritdoc />
    public Payment Create() => new();
}

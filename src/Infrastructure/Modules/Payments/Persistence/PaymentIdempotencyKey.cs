using Core.Modules.Payments.Intents;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>A reserved idempotency key.</summary>
public sealed class PaymentIdempotencyKey
{
    /// <summary>The row id.</summary>
    public Guid Id { get; set; }

    /// <summary>The owning customer.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>The caller's key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The request fingerprint.</summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>The reserved payment id.</summary>
    public Guid PaymentId { get; set; }

    /// <summary>When the key was reserved.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.MinValue;

    /// <summary>The currency of the reserved payment, kept for diagnostics.</summary>
    public string Currency { get; set; } = PaymentConstant.Defaults.Currency;
}

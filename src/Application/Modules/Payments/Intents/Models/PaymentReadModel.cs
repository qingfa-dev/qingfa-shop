using Core.Modules.Payments.Intents;

namespace Application.Modules.Payments.Intents.Models;

/// <summary>The projected payment row used by queries.</summary>
public sealed class PaymentReadModel
{
    /// <summary>The payment id.</summary>
    public Guid Id { get; set; }

    /// <summary>The provider.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>The provider reference.</summary>
    public string? ProviderReference { get; set; }

    /// <summary>The lifecycle status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The requested amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>The currency code.</summary>
    public string Currency { get; set; } = PaymentConstant.Defaults.Currency;

    /// <summary>The captured amount.</summary>
    public decimal CapturedAmount { get; set; }

    /// <summary>The refunded amount.</summary>
    public decimal RefundedAmount { get; set; }

    /// <summary>The caller's reference.</summary>
    public string ExternalReference { get; set; } = string.Empty;

    /// <summary>The idempotency key.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>The request fingerprint.</summary>
    public string IdempotencyFingerprint { get; set; } = string.Empty;

    /// <summary>The owning customer.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>The hosted-checkout URL.</summary>
    public string? ApprovalUrl { get; set; }

    /// <summary>When the intent expires, in UTC.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>When the payment was created, in UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the payment last changed, in UTC.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>The provider failure code.</summary>
    public string? FailureCode { get; set; }

    /// <summary>The provider failure message.</summary>
    public string? FailureMessage { get; set; }

    /// <summary>The event stream version.</summary>
    public int Version { get; set; }
}

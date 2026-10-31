using Core.Modules.Payments.Intents;

namespace Application.Modules.Payments.Refunds.Models;

/// <summary>The projected refund row.</summary>
public sealed class RefundReadModel
{
    /// <summary>The refund id.</summary>
    public Guid Id { get; set; }

    /// <summary>The payment id.</summary>
    public Guid PaymentId { get; set; }

    /// <summary>The refund status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The refunded amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>The currency code.</summary>
    public string Currency { get; set; } = PaymentConstant.Defaults.Currency;

    /// <summary>The provider refund reference.</summary>
    public string? ProviderReference { get; set; }

    /// <summary>The refund reason.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>The failure code.</summary>
    public string? FailureCode { get; set; }

    /// <summary>The failure message.</summary>
    public string? FailureMessage { get; set; }

    /// <summary>The idempotency key.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>When the refund was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the refund last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

using Core.Modules.Payments.Intents;

namespace Application.Modules.Payments.Intents.Models;

/// <summary>A monetary amount in the API contract.</summary>
public sealed record PaymentAmountModel(decimal Amount, string Currency);

/// <summary>The customer-facing view of a payment.</summary>
public record PaymentResponseModel
{
    /// <summary>The payment id.</summary>
    public Guid Id { get; init; }

    /// <summary>The provider name.</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>The lifecycle status.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>The requested amount.</summary>
    public PaymentAmountModel Amount { get; init; } = new(0, PaymentConstant.Defaults.Currency);

    /// <summary>The captured amount.</summary>
    public PaymentAmountModel CapturedAmount { get; init; } = new(0, PaymentConstant.Defaults.Currency);

    /// <summary>The refunded amount.</summary>
    public PaymentAmountModel RefundedAmount { get; init; } = new(0, PaymentConstant.Defaults.Currency);

    /// <summary>The caller's reference.</summary>
    public string ExternalReference { get; init; } = string.Empty;

    /// <summary>The hosted-checkout URL while the customer must act.</summary>
    public string? ApprovalUrl { get; init; }

    /// <summary>When the intent expires.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>When the payment was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>The failure code, when failed.</summary>
    public string? FailureCode { get; init; }

    /// <summary>The failure message, when failed.</summary>
    public string? FailureMessage { get; init; }
}

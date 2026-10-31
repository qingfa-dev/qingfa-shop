using Application.Modules.Payments.Intents.Models;

namespace Web.Api.Endpoints.Payments.Models;

/// <summary>The customer payment creation body.</summary>
public sealed record CreatePaymentBody
{
    /// <summary>The amount to charge.</summary>
    public PaymentAmountModel Amount { get; init; } = new(0, "USD");

    /// <summary>The provider name.</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>The caller's cart or order key.</summary>
    public string ExternalReference { get; init; } = string.Empty;

    /// <summary>Automatic or manual capture.</summary>
    public string CaptureMode { get; init; } = string.Empty;

    /// <summary>Where the customer returns after paying.</summary>
    public string? ReturnUrl { get; init; }

    /// <summary>Where the customer returns after cancelling.</summary>
    public string? CancelUrl { get; init; }
}

/// <summary>The admin capture body.</summary>
public sealed record CapturePaymentBody
{
    /// <summary>The amount to capture; null captures the remainder.</summary>
    public PaymentAmountModel? Amount { get; init; }
}

/// <summary>The admin refund body.</summary>
public sealed record RefundPaymentBody
{
    /// <summary>The amount to refund.</summary>
    public PaymentAmountModel Amount { get; init; } = new(0, "USD");

    /// <summary>Why the funds are being returned.</summary>
    public string Reason { get; init; } = string.Empty;
}

namespace Application.Modules.Payments.Ports.Models;

/// <summary>
/// Describes the operations and features supported by a payment gateway.
/// These are provider/account configuration capabilities, not payment eligibility guarantees.
/// </summary>
public sealed record PaymentGatewayCapabilities
{
    /// <summary>Whether the gateway supports refunds for less than the captured amount.</summary>
    public bool SupportsPartialRefund { get; init; }

    /// <summary>Whether the gateway supports manual capture after authorization.</summary>
    public bool SupportsManualCapture { get; init; }

    /// <summary>Whether the gateway supports webhook verification and processing.</summary>
    public bool SupportsWebhooks { get; init; }

    /// <summary>Capabilities of a gateway with no optional features enabled.</summary>
    public static PaymentGatewayCapabilities None { get; } = new();

    /// <summary>Capabilities of a gateway that supports every operation the module offers.</summary>
    public static PaymentGatewayCapabilities Full { get; } = new()
    {
        SupportsPartialRefund = true,
        SupportsManualCapture = true,
        SupportsWebhooks = true
    };
}

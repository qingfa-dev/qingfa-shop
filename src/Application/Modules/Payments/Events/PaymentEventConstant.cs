namespace Application.Modules.Payments.Events;

/// <summary>Stable wire names and versions for the payment integration events.</summary>
public static class PaymentEventConstant
{
    /// <summary>Routing key and event name for settled payments.</summary>
    public const string Captured = "payment.captured";

    /// <summary>Routing key and event name for settled refunds.</summary>
    public const string Refunded = "payment.refunded";

    /// <summary>Routing key and event name for failed payments.</summary>
    public const string Failed = "payment.failed";

    /// <summary>Payload version of every payment integration event.</summary>
    public const int Version = 1;
}

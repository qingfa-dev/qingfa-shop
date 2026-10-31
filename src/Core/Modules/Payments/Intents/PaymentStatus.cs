namespace Core.Modules.Payments.Intents;

/// <summary>The lifecycle state of a payment.</summary>
public enum PaymentStatus
{
    /// <summary>Intent created; the customer has not acted yet.</summary>
    Pending = 0,

    /// <summary>The provider is processing the payment.</summary>
    Processing = 1,

    /// <summary>Funds are held but not captured.</summary>
    Authorized = 2,

    /// <summary>Funds are captured.</summary>
    Captured = 3,

    /// <summary>Some captured funds were refunded.</summary>
    PartiallyRefunded = 4,

    /// <summary>All captured funds were refunded.</summary>
    Refunded = 5,

    /// <summary>The provider declined or the payment failed.</summary>
    Failed = 6,

    /// <summary>The payment was voided before capture.</summary>
    Cancelled = 7,

    /// <summary>The intent expired before the customer paid.</summary>
    Expired = 8
}
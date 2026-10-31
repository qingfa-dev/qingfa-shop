namespace Core.Modules.Payments.Intents;

/// <summary>A provider-observed outcome the aggregate can apply.</summary>
public enum PaymentObservation
{
    /// <summary>Funds are authorized.</summary>
    Authorized = 0,

    /// <summary>Funds are captured.</summary>
    Captured = 1,

    /// <summary>The payment failed.</summary>
    Failed = 2,

    /// <summary>The payment was voided.</summary>
    Cancelled = 3,

    /// <summary>The intent expired.</summary>
    Expired = 4
}
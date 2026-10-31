namespace Core.Modules.Payments.Refunds;

/// <summary>The state of a refund attempt.</summary>
public enum RefundStatus
{
    /// <summary>Requested but not yet confirmed by the provider.</summary>
    Pending = 0,

    /// <summary>The provider credited the funds.</summary>
    Succeeded = 1,

    /// <summary>The provider rejected the refund.</summary>
    Failed = 2
}
namespace Core.Modules.Payments.Refunds;

/// <summary>Converts refund statuses to and from their wire values.</summary>
public static class RefundStatusExtension
{
    /// <summary>Returns the lowercase wire value.</summary>
    public static string ToWire(this RefundStatus status) => status.ToString().ToLowerInvariant();
}
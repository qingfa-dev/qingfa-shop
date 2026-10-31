namespace Core.Modules.Payments.Refunds;

/// <summary>Converts refund statuses to and from their wire values.</summary>
public static class RefundStatusExtension
{
    /// <summary>Returns the lowercase wire value.</summary>
    public static string ToWire(this RefundStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Parses a wire value.</summary>
    public static RefundStatus FromWire(string value) => Enum.Parse<RefundStatus>(value, ignoreCase: true);
}
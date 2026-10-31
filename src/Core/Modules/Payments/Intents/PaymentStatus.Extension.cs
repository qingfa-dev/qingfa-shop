namespace Core.Modules.Payments.Intents;

/// <summary>Converts payment statuses to and from their wire values.</summary>
public static class PaymentStatusExtension
{
    /// <summary>Returns the lowercase wire value.</summary>
    public static string ToWire(this PaymentStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Parses a wire value.</summary>
    public static PaymentStatus FromWire(string value) => Enum.Parse<PaymentStatus>(value, ignoreCase: true);
}
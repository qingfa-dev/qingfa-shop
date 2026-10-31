namespace Core.Modules.Payments;

/// <summary>Converts provider names to and from their wire values.</summary>
public static class PaymentProviderExtension
{
    /// <summary>Returns the lowercase wire value.</summary>
    public static string ToWire(this PaymentProvider provider) => provider.ToString().ToLowerInvariant();

    /// <summary>Parses a wire value.</summary>
    public static PaymentProvider FromWire(string value) => Enum.Parse<PaymentProvider>(value, ignoreCase: true);
}
namespace Application.Modules.Payments.Ports.Models;

/// <summary>Maps provider intent states onto the event types the module settles.</summary>
public static class PaymentProviderEventTypeExtension
{
    /// <summary>Maps a provider intent state onto the observed event type.</summary>
    /// <param name="status">The provider-reported intent state.</param>
    /// <returns>The event type to settle.</returns>
    public static PaymentProviderEventType ToEventType(this PaymentProviderIntentStatus status) => status switch
    {
        PaymentProviderIntentStatus.Captured => PaymentProviderEventType.Captured,
        PaymentProviderIntentStatus.Authorized => PaymentProviderEventType.Authorized,
        PaymentProviderIntentStatus.Cancelled => PaymentProviderEventType.Cancelled,
        _ => PaymentProviderEventType.Failed
    };
}

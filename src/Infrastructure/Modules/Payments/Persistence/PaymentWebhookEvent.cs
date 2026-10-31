using Core.Modules.Payments.Intents;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>A recorded provider webhook delivery.</summary>
public sealed class PaymentWebhookEvent
{
    /// <summary>The row id.</summary>
    public Guid Id { get; set; }

    /// <summary>The provider name.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>The provider event id.</summary>
    public string ProviderEventId { get; set; } = string.Empty;

    /// <summary>The provider event type.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>The raw request body; never returned by an endpoint.</summary>
    public string RawBody { get; set; } = string.Empty;

    /// <summary>The processing outcome.</summary>
    public string Outcome { get; set; } = PaymentConstant.Outcomes.Received;

    /// <summary>When the delivery arrived.</summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>When processing finished.</summary>
    public DateTimeOffset? ProcessedAt { get; set; }
}

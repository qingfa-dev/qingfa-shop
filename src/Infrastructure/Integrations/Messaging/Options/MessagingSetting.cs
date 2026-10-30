namespace Infrastructure.Integrations.Messaging.Options;

/// <summary>Configuration contract for the Wolverine integration-event pipeline.</summary>
/// <remarks>
/// The outbox owns durability; these settings only select the transport and tune the
/// consumer's prefetch, concurrency, and retry behavior.
/// </remarks>
public sealed class MessagingSetting
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Messaging";

    /// <summary>Gets or sets whether the Wolverine bus replaces the no-op event bus.</summary>
    /// <value>Defaults to <c>true</c>.</value>
    public bool Enabled { get; set; } = MessagingDefaults.Defaults.Enabled;

    /// <summary>Gets or sets the transport used for publishing and consuming events.</summary>
    /// <value>Defaults to <see cref="MessagingTransport.InMemory"/>.</value>
    public MessagingTransport Transport { get; set; } = MessagingDefaults.Defaults.Transport;

    /// <summary>Gets or sets the topic exchange that carries integration events.</summary>
    /// <value>Defaults to <c>qingfa.events</c>.</value>
    public string ExchangeName { get; set; } = MessagingDefaults.Defaults.ExchangeName;

    /// <summary>Gets or sets the prefix for per-consumer queues.</summary>
    /// <value>Defaults to <c>qingfa</c>.</value>
    public string QueuePrefix { get; set; } = MessagingDefaults.Defaults.QueuePrefix;

    /// <summary>Gets or sets the number of unacknowledged messages a consumer may hold.</summary>
    /// <value>Defaults to <c>16</c>; must be between 1 and 65535.</value>
    public int PrefetchCount { get; set; } = MessagingDefaults.Defaults.PrefetchCount;

    /// <summary>Gets or sets the maximum parallel message count per listener.</summary>
    /// <value>Defaults to <c>8</c>; must be positive.</value>
    public int MaxConcurrency { get; set; } = MessagingDefaults.Defaults.MaxConcurrency;

    /// <summary>Gets or sets the number of delivery attempts before dead-lettering.</summary>
    /// <value>Defaults to <c>5</c>; must be positive.</value>
    public int MaxAttempts { get; set; } = MessagingDefaults.Defaults.MaxAttempts;

    /// <summary>Gets or sets the base delay, in seconds, for the exponential retry backoff.</summary>
    /// <value>Defaults to <c>2</c>; must be positive.</value>
    public int RetryDelaySeconds { get; set; } = MessagingDefaults.Defaults.RetryDelaySeconds;
}

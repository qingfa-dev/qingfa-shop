namespace Infrastructure.Integrations.Messaging.Options;

/// <summary>Default values and bounds for the <c>Messaging</c> configuration section.</summary>
/// <remarks>
/// Defaults keep local and test hosts on the in-process transport. A broker is only contacted
/// when <see cref="MessagingSetting.Transport"/> selects <see cref="MessagingTransport.RabbitMq"/>.
/// </remarks>
public static class MessagingDefaults
{
    /// <summary>Default configuration values.</summary>
    public static class Defaults
    {
        /// <summary>Messaging is enabled by default so the outbox reaches a real bus.</summary>
        public const bool Enabled = true;

        /// <summary>The in-process transport is the safe default for hosts without a broker.</summary>
        public const MessagingTransport Transport = MessagingTransport.InMemory;

        /// <summary>The topic exchange that carries integration events.</summary>
        public const string ExchangeName = "qingfa.events";

        /// <summary>The prefix for per-consumer queues.</summary>
        public const string QueuePrefix = "qingfa";

        /// <summary>The number of unacknowledged messages a consumer may hold.</summary>
        public const int PrefetchCount = 16;

        /// <summary>The maximum number of messages a consumer processes concurrently.</summary>
        public const int MaxConcurrency = 8;

        /// <summary>The number of delivery attempts before a message is dead-lettered.</summary>
        public const int MaxAttempts = 5;

        /// <summary>The base delay, in seconds, for the exponential retry backoff.</summary>
        public const int RetryDelaySeconds = 2;
    }

    /// <summary>Validation bounds enforced by <c>MessagingSettingValidator</c>.</summary>
    public static class Constraints
    {
        /// <summary>RabbitMQ prefetch is a 16-bit unsigned value.</summary>
        public const int PrefetchCountMax = ushort.MaxValue;

        /// <summary>Upper bound that keeps a misconfigured host from exhausting connections.</summary>
        public const int MaxConcurrencyMax = 1024;

        /// <summary>Upper bound for delivery attempts; keeps the exponential schedule finite.</summary>
        public const int MaxAttemptsMax = 20;

        /// <summary>Upper bound for the retry base delay, in seconds.</summary>
        public const int RetryDelaySecondsMax = 3600;
    }
}

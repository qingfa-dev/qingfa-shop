namespace Infrastructure.BuildingBlocks.Adapters.Messaging.Options;

/// <summary>Validation failures and stable codes for <see cref="MessagingSetting"/>.</summary>
public static class MessagingSettingOutcome
{
    /// <summary>Failure errors raised when the messaging settings are invalid.</summary>
    public static class Failure
    {
        /// <summary>The transport is not a known value.</summary>
        public static Error TransportUnknown => Error.Validation(
            code: Codes.TransportUnknown,
            message: "Messaging:Transport must be a known transport.");

        /// <summary>The exchange name is missing.</summary>
        public static Error ExchangeNameRequired => Error.Validation(
            code: Codes.ExchangeNameRequired,
            message: "Messaging:ExchangeName is required.");

        /// <summary>The queue prefix is missing.</summary>
        public static Error QueuePrefixRequired => Error.Validation(
            code: Codes.QueuePrefixRequired,
            message: "Messaging:QueuePrefix is required.");

        /// <summary>The prefetch count is outside the allowed range.</summary>
        public static Error PrefetchCountOutOfRange => Error.Validation(
            code: Codes.PrefetchCountOutOfRange,
            message:
            $"Messaging:PrefetchCount must be between {MessagingDefaults.Constraints.PrefetchCountMin} and {MessagingDefaults.Constraints.PrefetchCountMax}.");

        /// <summary>The maximum concurrency is outside the allowed range.</summary>
        public static Error MaxConcurrencyOutOfRange => Error.Validation(
            code: Codes.MaxConcurrencyOutOfRange,
            message:
            $"Messaging:MaxConcurrency must be between {MessagingDefaults.Constraints.MaxConcurrencyMin} and {MessagingDefaults.Constraints.MaxConcurrencyMax}.");

        /// <summary>The delivery attempt count is outside the allowed range.</summary>
        public static Error MaxAttemptsOutOfRange => Error.Validation(
            code: Codes.MaxAttemptsOutOfRange,
            message:
            $"Messaging:MaxAttempts must be between {MessagingDefaults.Constraints.MaxAttemptsMin} and {MessagingDefaults.Constraints.MaxAttemptsMax}.");

        /// <summary>The retry delay is outside the allowed range.</summary>
        public static Error RetryDelaySecondsOutOfRange => Error.Validation(
            code: Codes.RetryDelaySecondsOutOfRange,
            message:
            $"Messaging:RetryDelaySeconds must be between {MessagingDefaults.Constraints.RetryDelaySecondsMin} and {MessagingDefaults.Constraints.RetryDelaySecondsMax}.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string TransportUnknown = "Messaging.Transport.Unknown";
        public const string ExchangeNameRequired = "Messaging.ExchangeName.Required";
        public const string QueuePrefixRequired = "Messaging.QueuePrefix.Required";
        public const string PrefetchCountOutOfRange = "Messaging.PrefetchCount.OutOfRange";
        public const string MaxConcurrencyOutOfRange = "Messaging.MaxConcurrency.OutOfRange";
        public const string MaxAttemptsOutOfRange = "Messaging.MaxAttempts.OutOfRange";
        public const string RetryDelaySecondsOutOfRange = "Messaging.RetryDelaySeconds.OutOfRange";
    }
}

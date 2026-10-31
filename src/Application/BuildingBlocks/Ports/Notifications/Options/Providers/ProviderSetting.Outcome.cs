namespace Application.BuildingBlocks.Ports.Notifications.Options.Providers;

/// <summary>Error definitions for provider configuration validation.</summary>
public static class ProviderOutcome
{
    /// <summary>Builds the stable codes for provider configuration failures.</summary>
    public static class Codes
    {
        /// <summary>Code for an out-of-range priority.</summary>
        public static string PriorityOutOfRange(string providerName) =>
            $"Provider.{providerName}.Priority.OutOfRange";

        /// <summary>Code for an out-of-range retry count.</summary>
        public static string RetryCountOutOfRange(string providerName) =>
            $"Provider.{providerName}.RetryCount.OutOfRange";

        /// <summary>Code for an out-of-range timeout.</summary>
        public static string TimeoutOutOfRange(string providerName) =>
            $"Provider.{providerName}.Timeout.OutOfRange";

        /// <summary>Code for a missing provider section.</summary>
        public static string SectionRequired(string providerName) =>
            $"Provider.{providerName}.Section.Required";
    }

    /// <summary>Provider configuration error factories.</summary>
    public static class Failure
    {
        /// <summary>Provider priority is outside the allowed range.</summary>
        public static Error PriorityOutOfRange(string providerName) => Error.Validation(
            code: Codes.PriorityOutOfRange(providerName),
            message: $"Provider {providerName} Priority must be between {ProviderSettingConstant.Constraints.MinPriority} and {ProviderSettingConstant.Constraints.MaxPriority}.");

        /// <summary>Provider retry count is outside the allowed range.</summary>
        public static Error RetryCountOutOfRange(string providerName) => Error.Validation(
            code: Codes.RetryCountOutOfRange(providerName),
            message: $"Provider {providerName} RetryCount must be between {ProviderSettingConstant.Constraints.MinRetryCount} and {ProviderSettingConstant.Constraints.MaxRetryCount}.");

        /// <summary>Provider timeout is outside the allowed range.</summary>
        public static Error TimeoutOutOfRange(string providerName) => Error.Validation(
            code: Codes.TimeoutOutOfRange(providerName),
            message: $"Provider {providerName} Timeout must be between {ProviderSettingConstant.Constraints.MinTimeoutSeconds} and {ProviderSettingConstant.Constraints.MaxTimeoutSeconds} seconds.");

        /// <summary>Provider configuration section is required.</summary>
        public static Error SectionRequired(string providerName) => Error.Validation(
            code: Codes.SectionRequired(providerName),
            message: $"Provider {providerName} Section is required.");
    }
}

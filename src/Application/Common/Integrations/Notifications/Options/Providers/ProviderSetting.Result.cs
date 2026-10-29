namespace Application.Common.Integrations.Notifications.Options.Providers;

/// <summary>Error definitions for provider configuration validation.</summary>
public static class ProviderResult
{
    /// <summary>Provider configuration error factories.</summary>
    public static class Failure
    {
        /// <summary>Provider priority is outside the allowed range.</summary>
        public static Error PriorityOutOfRange(string providerName) => Error.Validation(
            code: $"Provider.{providerName}.Priority.OutOfRange",
            message: $"Provider {providerName} Priority must be between {ProviderSettingConstant.Constraints.MinPriority} and {ProviderSettingConstant.Constraints.MaxPriority}.");

        /// <summary>Provider retry count is outside the allowed range.</summary>
        public static Error RetryCountOutOfRange(string providerName) => Error.Validation(
            code: $"Provider.{providerName}.RetryCount.OutOfRange",
            message: $"Provider {providerName} RetryCount must be between {ProviderSettingConstant.Constraints.MinRetryCount} and {ProviderSettingConstant.Constraints.MaxRetryCount}.");

        /// <summary>Provider timeout is outside the allowed range.</summary>
        public static Error TimeoutOutOfRange(string providerName) => Error.Validation(
            code: $"Provider.{providerName}.Timeout.OutOfRange",
            message: $"Provider {providerName} Timeout must be between {ProviderSettingConstant.Constraints.MinTimeoutSeconds} and {ProviderSettingConstant.Constraints.MaxTimeoutSeconds} seconds.");

        /// <summary>Provider configuration section is required.</summary>
        public static Error SectionRequired(string providerName) => Error.Validation(
            code: $"Provider.{providerName}.Section.Required",
            message: $"Provider {providerName} Section is required.");
    }
}

namespace Application.BuildingBlocks.Ports.Notifications.Options.Providers;

/// <summary>Extension methods for validating provider configuration settings.</summary>
public static class ProviderExtensions
{
    /// <summary>Validates common provider setting constraints (priority, retry count, timeout).</summary>
    /// <param name="provider">The provider setting to validate.</param>
    /// <returns>A sequence of validation errors, or empty if valid.</returns>
    public static IEnumerable<Error> ValidateBase(this IProviderSetting provider)
    {
        string providerKey = provider.GetType().Name;

        // Validate: Priority must be within configured range
        if (provider.Priority < ProviderSettingConstant.Constraints.MinPriority ||
            provider.Priority > ProviderSettingConstant.Constraints.MaxPriority)
            yield return ProviderOutcome.Failure.PriorityOutOfRange(providerKey);

        // Validate: RetryCount must be within configured range
        if (provider.RetryCount < ProviderSettingConstant.Constraints.MinRetryCount ||
            provider.RetryCount > ProviderSettingConstant.Constraints.MaxRetryCount)
            yield return ProviderOutcome.Failure.RetryCountOutOfRange(providerKey);

        // Validate: Timeout must be within configured range
        if (provider.Timeout.TotalSeconds < ProviderSettingConstant.Constraints.MinTimeoutSeconds ||
            provider.Timeout.TotalSeconds > ProviderSettingConstant.Constraints.MaxTimeoutSeconds)
            yield return ProviderOutcome.Failure.TimeoutOutOfRange(providerKey);
    }
}

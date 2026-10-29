namespace Application.Common.Integrations.Notifications.Options.Providers;

/// <summary>Base class for notification provider configuration settings.</summary>
public abstract class BaseProviderSetting : IProviderSetting
{
    /// <summary>Gets the configuration section path for this provider.</summary>
    public static string Section => string.Empty;
    /// <summary>Gets or sets whether this provider is enabled for notification delivery.</summary>
    public bool Enabled { get; set; } = ProviderSettingConstant.Defaults.Enabled;
    /// <summary>Gets or sets the provider execution priority (lower = higher priority).</summary>
    public int Priority { get; set; } = ProviderSettingConstant.Defaults.Priority;
    /// <summary>Gets or sets the number of retry attempts for transient failures.</summary>
    public int RetryCount { get; set; } = ProviderSettingConstant.Defaults.RetryCount;
    /// <summary>Gets or sets the request timeout duration.</summary>
    public TimeSpan Timeout { get; set; } = ProviderSettingConstant.DefaultTimeout;
}

namespace Application.Common.Integrations.Notifications.Options.Providers;

/// <summary>
/// Common contract for all provider configurations.
/// </summary>
public interface IProviderSetting
{
    static abstract string Section { get; }
    bool Enabled { get; set; }
    int Priority { get; set; }
    int RetryCount { get; set; }
    TimeSpan Timeout { get; set; }
}

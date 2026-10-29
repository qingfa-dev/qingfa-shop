namespace Application.Common.Integrations.Notifications.Options.Channels;

/// <summary>
/// Common contract for all notification channel configurations.
/// </summary>
public interface IChannelSetting
{
    static abstract string Section { get; }
    bool Enabled { get; }
}

namespace Application.BuildingBlocks.Ports.Notifications.Options.Channels;

/// <summary>
/// Common contract for all notification channel configurations.
/// </summary>
public interface IChannelSetting
{
    static abstract string Section { get; }
    bool Enabled { get; }
}

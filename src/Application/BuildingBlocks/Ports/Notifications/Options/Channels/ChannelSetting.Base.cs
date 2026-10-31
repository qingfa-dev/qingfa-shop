namespace Application.BuildingBlocks.Ports.Notifications.Options.Channels;

/// <summary>Base class for notification channel configuration settings.</summary>
public abstract class ChannelSettingBase : IChannelSetting
{
    /// <inheritdoc />
    public static string Section => string.Empty;
    /// <summary>Gets or sets whether this channel is enabled for notification delivery.</summary>
    public bool Enabled { get; set; } = ChannelConstant.Defaults.Enabled;
}

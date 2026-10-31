using Application.BuildingBlocks.Ports.Notifications.Options;

using Application.BuildingBlocks.Ports.Notifications.Options.Channels;

namespace Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Options;

/// <summary>
/// Contains general configuration settings for the email notification channel.
/// </summary>
public sealed class EmailChannelSetting : ChannelSettingBase
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public static new string Section => $"{NotificationSetting.Section}:Channels:Email";

    /// <summary>
    /// Gets or sets the default sender email address.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default sender display name.
    /// </summary>
    public string FromName { get; set; } = string.Empty;
}

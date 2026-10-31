using Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Options;
using Application.BuildingBlocks.Ports.Notifications.Options.Providers;

namespace Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Providers.SendGrid;

/// <summary>
/// Configuration settings for the SendGrid email provider.
/// </summary>
public sealed class SendGridProviderSetting : BaseProviderSetting
{
    /// <inheritdoc />
        public static new string Section => $"{EmailChannelSetting.Section}:Providers:SendGrid";

    /// <inheritdoc />
    public new int Priority { get; set; } = 2;

    /// <summary>
    /// Gets or sets the API key used to authenticate with SendGrid.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

}

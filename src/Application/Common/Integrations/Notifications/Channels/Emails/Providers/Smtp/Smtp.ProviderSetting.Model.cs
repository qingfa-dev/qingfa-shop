using Application.Common.Integrations.Notifications.Channels.Emails.Options;
using Application.Common.Integrations.Notifications.Options.Providers;

namespace Application.Common.Integrations.Notifications.Channels.Emails.Providers.Smtp;

/// <summary>
/// Configuration settings for the SMTP email provider.
/// </summary>
public sealed class SmtpProviderSetting : BaseProviderSetting
{
    /// <inheritdoc />
    public static new string Section => $"{EmailChannelSetting.Section}:Providers:Smtp";
    /// <inheritdoc />
    public new int Priority { get; set; } = 1;

    /// <summary>
    /// Gets or sets the SMTP server host address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the SMTP server port.
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    /// Gets or sets a value indicating whether SSL is enabled for the connection.
    /// </summary>
    public bool EnableSsl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use default network credentials.
    /// </summary>
    public bool UseDefaultCredentials { get; set; } = true;

    /// <summary>
    /// Gets or sets the username for SMTP authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for SMTP authentication.
    /// </summary>
    public string? Password { get; set; }
}

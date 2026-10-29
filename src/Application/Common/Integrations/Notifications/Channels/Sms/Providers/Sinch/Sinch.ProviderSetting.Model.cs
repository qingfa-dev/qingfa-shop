using Application.Common.Integrations.Notifications.Channels.Sms.Options;
using Application.Common.Integrations.Notifications.Options.Providers;

namespace Application.Common.Integrations.Notifications.Channels.Sms.Providers.Sinch;

/// <summary>Configuration settings for the Sinch SMS provider.</summary>
public sealed class SinchProviderSetting : BaseProviderSetting
{
    /// <inheritdoc />
    public static new string Section => $"{SmsChannelSetting.Section}:Providers:Sinch";

    public new int Priority { get; set; } = 1;

    /// <summary>Gets or sets the Sinch project identifier.</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Gets or sets the Sinch API key identifier.</summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>Gets or sets the Sinch API key secret.</summary>
    public string KeySecret { get; set; } = string.Empty;

    /// <summary>Gets or sets the sender phone number in E.164 format.</summary>
    public string SenderPhoneNumber { get; set; } = string.Empty;
}

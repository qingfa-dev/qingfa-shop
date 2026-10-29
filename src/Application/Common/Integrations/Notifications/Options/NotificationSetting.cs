namespace Application.Common.Integrations.Notifications.Options;

/// <summary>Root configuration settings for the notification system.</summary>
public sealed class NotificationSetting
{
    public const string Section = "Notification";

    /// <summary>Gets or sets whether background job processing is enabled for notification dispatch.</summary>
    public bool EnableBackgroundJobs { get; set; }
    /// <summary>Gets or sets the application system name used in notification templates.</summary>
    public string ApplicationName { get; set; } = NotificationSettingConstant.Defaults.ApplicationName;
    /// <summary>Gets or sets the support email address displayed in notifications.</summary>
    public string SupportEmail { get; set; } = NotificationSettingConstant.Defaults.SupportEmail;
    /// <summary>Gets or sets the support phone number displayed in notifications.</summary>
    public string SupportPhone { get; set; } = NotificationSettingConstant.Defaults.SupportPhone;
    /// <summary>Gets or sets the customer support link URL.</summary>
    public string CustomerSupportLink { get; set; } = NotificationSettingConstant.Defaults.CustomerSupportLink;
    /// <summary>Gets or sets the application base URL.</summary>
    public string ApplicationUrl { get; set; } = NotificationSettingConstant.Defaults.ApplicationUrl;
    /// <summary>Gets or sets the unsubscribe URL for email notifications.</summary>
    public string UnsubscribeUrl { get; set; } = NotificationSettingConstant.Defaults.UnsubscribeUrl;
    /// <summary>Gets or sets the survey URL for feedback collection.</summary>
    public string SurveyUrl { get; set; } = NotificationSettingConstant.Defaults.SurveyUrl;
}
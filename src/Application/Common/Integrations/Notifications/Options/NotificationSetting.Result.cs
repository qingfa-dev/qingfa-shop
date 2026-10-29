namespace Application.Common.Integrations.Notifications.Options;

/// <summary>Error definitions for root notification system configuration validation.</summary>
public static class NotificationSettingResult
{
    /// <summary>Notification configuration error factories.</summary>
    public static class Failure
    {
        /// <summary>ApplicationName is required and must not be empty.</summary>
        public static Error ApplicationNameRequired => Error.Validation(
            code: "Notifications.ApplicationName.Required",
            message: "Notifications ApplicationName is required.");

        /// <summary>SupportEmail must be a valid email address format.</summary>
        public static Error InvalidSupportEmail => Error.Validation(
            code: "Notifications.SupportEmail.Invalid",
            message: "Notifications SupportEmail must be a valid email address.");

        /// <summary>SupportPhone must be a valid phone number format.</summary>
        public static Error InvalidSupportPhone => Error.Validation(
            code: "Notifications.SupportPhone.Invalid",
            message: "Notifications SupportPhone must be a valid phone number.");

        /// <summary>ApplicationUrl must be a valid absolute URL.</summary>
        public static Error InvalidApplicationUrl => Error.Validation(
            code: "Notifications.ApplicationUrl.Invalid",
            message: "Notifications ApplicationUrl must be a valid absolute URL.");

        /// <summary>CustomerSupportLink must be a valid absolute URL.</summary>
        public static Error InvalidCustomerSupportLink => Error.Validation(
            code: "Notifications.CustomerSupportLink.Invalid",
            message: "Notifications CustomerSupportLink must be a valid absolute URL.");

        /// <summary>UnsubscribeUrl must be a valid absolute URL.</summary>
        public static Error InvalidUnsubscribeUrl => Error.Validation(
            code: "Notifications.UnsubscribeUrl.Invalid",
            message: "Notifications UnsubscribeUrl must be a valid absolute URL.");

        /// <summary>SurveyUrl must be a valid absolute URL.</summary>
        public static Error InvalidSurveyUrl => Error.Validation(
            code: "Notifications.SurveyUrl.Invalid",
            message: "Notifications SurveyUrl must be a valid absolute URL.");
    }
}

namespace Application.BuildingBlocks.Ports.Notifications.Options;

/// <summary>Error definitions for root notification system configuration validation.</summary>
public static class NotificationSettingOutcome
{
    /// <summary>Notification configuration error factories.</summary>
    public static class Failure
    {
        /// <summary>ApplicationName is required and must not be empty.</summary>
        public static Error ApplicationNameRequired => Error.Validation(
            code: Codes.ApplicationNameRequired,
            message: "Notifications ApplicationName is required.");

        /// <summary>SupportEmail must be a valid email address format.</summary>
        public static Error InvalidSupportEmail => Error.Validation(
            code: Codes.InvalidSupportEmail,
            message: "Notifications SupportEmail must be a valid email address.");

        /// <summary>SupportPhone must be a valid phone number format.</summary>
        public static Error InvalidSupportPhone => Error.Validation(
            code: Codes.InvalidSupportPhone,
            message: "Notifications SupportPhone must be a valid phone number.");

        /// <summary>ApplicationUrl must be a valid absolute URL.</summary>
        public static Error InvalidApplicationUrl => Error.Validation(
            code: Codes.InvalidApplicationUrl,
            message: "Notifications ApplicationUrl must be a valid absolute URL.");

        /// <summary>CustomerSupportLink must be a valid absolute URL.</summary>
        public static Error InvalidCustomerSupportLink => Error.Validation(
            code: Codes.InvalidCustomerSupportLink,
            message: "Notifications CustomerSupportLink must be a valid absolute URL.");

        /// <summary>UnsubscribeUrl must be a valid absolute URL.</summary>
        public static Error InvalidUnsubscribeUrl => Error.Validation(
            code: Codes.InvalidUnsubscribeUrl,
            message: "Notifications UnsubscribeUrl must be a valid absolute URL.");

        /// <summary>SurveyUrl must be a valid absolute URL.</summary>
        public static Error InvalidSurveyUrl => Error.Validation(
            code: Codes.InvalidSurveyUrl,
            message: "Notifications SurveyUrl must be a valid absolute URL.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ApplicationNameRequired = "Notifications.ApplicationName.Required";
        public const string InvalidSupportEmail = "Notifications.SupportEmail.Invalid";
        public const string InvalidSupportPhone = "Notifications.SupportPhone.Invalid";
        public const string InvalidApplicationUrl = "Notifications.ApplicationUrl.Invalid";
        public const string InvalidCustomerSupportLink = "Notifications.CustomerSupportLink.Invalid";
        public const string InvalidUnsubscribeUrl = "Notifications.UnsubscribeUrl.Invalid";
        public const string InvalidSurveyUrl = "Notifications.SurveyUrl.Invalid";
    }
}

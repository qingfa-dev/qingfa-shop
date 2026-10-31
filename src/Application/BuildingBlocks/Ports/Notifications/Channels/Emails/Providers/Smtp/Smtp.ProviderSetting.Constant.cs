using System.Text.RegularExpressions;

namespace Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Providers.Smtp;

/// <summary>Constants and constraints for SMTP provider configuration.</summary>
public static class SmtpProviderSettingConstant
{
    /// <summary>Constraint boundaries for SMTP configuration values.</summary>
    public static class Constraints
    {
        public const int PortMin = 1;
        public const int PortMax = 65535;
        public const int HostMaxLength = 256;
        public const int UsernameMaxLength = 128;
        public const int PasswordMaxLength = 256;
    }

    /// <summary>Default configuration values for the SMTP provider.</summary>
    public static class Defaults
    {
        public const string Section = "Notification:Channels:Email:Providers:Smtp";
        public const int Priority = 1;
        public const string Host = "localhost";
        public const int Port = 25;
        public const bool EnableSsl = false;
        public const bool UseDefaultCredentials = true;
    }

    /// <summary>Regex patterns for SMTP hostname validation.</summary>
    public static class Patterns
    {
        public static readonly Regex HostName = new(
            @"^([a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$|^localhost$|^\d{1,3}(\.\d{1,3}){3}$",
            RegexOptions.NonBacktracking,
            TimeSpan.FromMilliseconds(100));
    }
}

namespace Infrastructure.Security.Identity.Domain.Users;

/// <summary>
/// Contains constant values for the User domain.
/// </summary>
public static class UserConstant
{
    /// <summary>
    /// Default values for user properties.
    /// </summary>
    public static class Defaults
    {
        public const bool IsActive = true;
        public const bool EmailConfirmed = false;
        public const bool PhoneNumberConfirmed = false;
        public const int AccessFailedCount = 0;
    }

    /// <summary>
    /// Validation constraints for user properties.
    /// </summary>
    public static class Constraints
    {
        public static class Username
        {
            public const int MinLength = 3;
            public const int MaxLength = 32;
        }

        public static class Email
        {
            public const int MaxLength = 254;
        }

        public static class Name
        {
            public const int MaxFirstNameLength = 50;
            public const int MaxLastNameLength = 50;
        }

        public static class Phone
        {
            public const int MaxLength = 15;
        }

        public static class Password
        {
            public const int MinLength = 12;
            public const int MaxLength = 128;
            public const int MaxHashLength = 1024;
        }

        public static class Age
        {
            public const int Minimum = 13;
            public const int Maximum = 120;
        }

        public static class Otp
        {
            public const int MinLength = 6;
            public const int MaxLength = 10;
        }
    }

    /// <summary>
    /// Validation regex patterns.
    /// </summary>
    public static class Patterns
    {
        public static class Email
        {
            /// <summary>
            /// Standard email format pattern.
            /// </summary>
            public const string Regex =
                @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        }

        public static class Username
        {
            public const string Regex =
                @"^[a-zA-Z0-9](?:[a-zA-Z0-9._-]{1,30}[a-zA-Z0-9])?$";
        }

        public static class Phone
        {
            /// <summary>
            /// E.164 international phone format.
            /// Example: +14155552671
            /// </summary>
            public const string Regex =
                @"^\+[1-9]\d{1,14}$";
        }

        public static class Password
        {
            /// <summary>
            /// At least one uppercase letter,
            /// one lowercase letter,
            /// one digit,
            /// and 12-128 characters.
            /// </summary>
            public const string Regex =
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{12,128}$";

            public const string Uppercase = @"[A-Z]";
            public const string Lowercase = @"[a-z]";
            public const string Digit = @"\d";
            public const string SpecialChar = @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]";
        }

        public static class Otp
        {
            public const string Regex = @"^\d{6}$";
        }
    }

    /// <summary>
    /// OTP runtime configuration.
    /// </summary>
    public static class Otp
    {
        public const int ValidityDurationMinutes = 10;
        public const int MaxResendAttempts = 3;

        public const string CachePrefix = "otp:phone:";
    }
}
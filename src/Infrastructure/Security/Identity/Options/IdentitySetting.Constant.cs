namespace Infrastructure.Security.Identity.Options;

/// <summary>
/// Contains constant values for Identity security settings and policies.
/// </summary>
public static class IdentitySettingConstant
{
    /// <summary>
    /// Configuration for security stamp validation.
    /// </summary>
    public static class SecurityStamp
    {
        /// <summary>
        /// The interval at which security stamps are validated for fresh identity checks.
        /// </summary>
        public static readonly TimeSpan ValidationInterval = TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Configuration for Data Protection.
    /// </summary>
    public static class DataProtection
    {
        /// <summary>
        /// The application name used for data protection to ensure token stability.
        /// </summary>
        public const string ApplicationName = "ReSys.Shop";
    }

    /// <summary>
    /// Configuration for Identity password policies (OWASP aligned).
    /// </summary>
    public static class Password
    {
        public const bool RequireDigit = true;
        public const bool RequireLowercase = true;
        public const bool RequireUppercase = true;
        public const bool RequireNonAlphanumeric = true;
        public const int RequiredLength = 12;
        public const int RequiredUniqueChars = 3;
    }

    /// <summary>
    /// Configuration for Identity user policies.
    /// </summary>
    public static class User
    {
        public const bool RequireUniqueEmail = true;
        public const string AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    }

    /// <summary>
    /// Configuration for Identity lockout policies.
    /// </summary>
    public static class Lockout
    {
        /// <summary>
        /// The default duration a user is locked out after reaching the maximum failed attempts.
        /// </summary>
        public static readonly TimeSpan DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        public const int MaxFailedAccessAttempts = 5;
        public const bool AllowedForNewUsers = true;
    }

    /// <summary>
    /// Configuration for Identity sign-in requirements.
    /// </summary>
    public static class SignIn
    {
        public const bool RequireConfirmedEmail = false;
        public const bool RequireConfirmedAccount = false;
    }
}

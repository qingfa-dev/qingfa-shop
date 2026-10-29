namespace Application.Common.Security.Authentication.Identity;

/// <summary>Constants shared by the Identity API application services.</summary>
public static class IdentityConstant
{
    /// <summary>Identity API route fragments.</summary>
    public static class Routes
    {
        /// <summary>Base route for identity endpoints.</summary>
        public const string Base = "/identity";

        /// <summary>Registration endpoint.</summary>
        public const string Register = "/register";

        /// <summary>Login endpoint.</summary>
        public const string Login = "/login";

        /// <summary>Refresh endpoint.</summary>
        public const string Refresh = "/refresh";

        /// <summary>Logout endpoint.</summary>
        public const string Logout = "/logout";

        /// <summary>Email confirmation endpoint.</summary>
        public const string ConfirmEmail = "/confirmEmail";

        /// <summary>Confirmation resend endpoint.</summary>
        public const string ResendConfirmation = "/resendConfirmationEmail";

        /// <summary>Forgot-password endpoint.</summary>
        public const string ForgotPassword = "/forgotPassword";

        /// <summary>Password reset endpoint.</summary>
        public const string ResetPassword = "/resetPassword";

        /// <summary>Current-user information endpoint.</summary>
        public const string ManageInfo = "/manage/info";

        /// <summary>Two-factor management endpoint.</summary>
        public const string ManageTwoFactor = "/manage/2fa";

        /// <summary>External provider discovery endpoint.</summary>
        public const string ExternalProviders = "/external/providers";

        /// <summary>External challenge endpoint prefix.</summary>
        public const string ExternalChallenge = "/external/challenge";

        /// <summary>External callback endpoint.</summary>
        public const string ExternalCallback = "/external/callback";
    }

    /// <summary>External authentication scheme names.</summary>
    public static class Providers
    {
        /// <summary>Google OAuth scheme.</summary>
        public const string Google = "Google";

        /// <summary>Microsoft account OAuth scheme.</summary>
        public const string Microsoft = "Microsoft";

        /// <summary>Generic OpenID Connect scheme.</summary>
        public const string OpenIdConnect = "OpenIdConnect";
    }

    /// <summary>ASP.NET Core Identity token provider names.</summary>
    public static class TokenProviders
    {
        /// <summary>Email confirmation and password reset provider.</summary>
        public const string Email = "Email";

        /// <summary>Authenticator token provider.</summary>
        public const string Authenticator = "Authenticator";
    }

    /// <summary>Validation limits for identity requests.</summary>
    public static class Constraints
    {
        /// <summary>Minimum username length.</summary>
        public const int MinUserNameLength = 3;

        /// <summary>Maximum username length.</summary>
        public const int MaxUserNameLength = 64;

        /// <summary>Maximum email length.</summary>
        public const int MaxEmailLength = 256;

        /// <summary>Minimum password length.</summary>
        public const int MinPasswordLength = 12;

        /// <summary>Maximum password length.</summary>
        public const int MaxPasswordLength = 128;

        /// <summary>Maximum first-name length.</summary>
        public const int MaxFirstNameLength = 50;

        /// <summary>Maximum last-name length.</summary>
        public const int MaxLastNameLength = 50;

        /// <summary>Number of recovery codes issued for two-factor authentication.</summary>
        public const int RecoveryCodeCount = 10;
    }

    /// <summary>Default values used by identity services.</summary>
    public static class Defaults
    {
        /// <summary>Role assigned to newly registered users.</summary>
        public const string DefaultRole = "User";

        /// <summary>Default identity API section name.</summary>
        public const string SectionName = "IdentityApi";

        /// <summary>Default external authentication section name.</summary>
        public const string ExternalSectionName = "ExternalAuth";

        /// <summary>Default OIDC scopes.</summary>
        public static readonly string[] OidcScopes = ["openid", "profile", "email"];
    }
}

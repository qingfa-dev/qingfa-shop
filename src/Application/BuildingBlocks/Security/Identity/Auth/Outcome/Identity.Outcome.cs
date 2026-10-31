namespace Application.BuildingBlocks.Security.Identity.Auth.Outcome;

/// <summary>Failures and success messages for identity application services.</summary>
public static class IdentityOutcome
{
    /// <summary>Success messages for identity operations.</summary>
    public static class Success
    {
        /// <summary>Registration succeeded.</summary>
        public const string Registered = "User registered successfully.";

        /// <summary>Email confirmation succeeded.</summary>
        public const string EmailConfirmed = "Email confirmed successfully.";

        /// <summary>Password reset succeeded.</summary>
        public const string PasswordReset = "Password reset successfully.";

        /// <summary>Logout succeeded.</summary>
        public const string LoggedOut = "Logged out successfully.";

        /// <summary>Two-factor state changed.</summary>
        public const string TwoFactorUpdated = "Two-factor settings updated.";
    }

    /// <summary>Failure errors for identity operations.</summary>
    public static class Failure
    {
        /// <summary>The user is missing.</summary>
        public static Error UserNotFound => Error.NotFound(
            code: Codes.UserNotFound,
            message: "The specified user was not found.");

        /// <summary>The user entity is missing.</summary>
        public static Error UserRequired => Error.UnprocessableEntity(
            code: Codes.UserRequired,
            message: "An identity user is required.");

        /// <summary>The username is already registered.</summary>
        public static Error UserNameTaken => Error.Conflict(
            code: Codes.UserNameTaken,
            message: "The username is already registered.");

        /// <summary>The email is already registered.</summary>
        public static Error EmailTaken => Error.Conflict(
            code: Codes.EmailTaken,
            message: "The email address is already registered.");

        /// <summary>The supplied credentials are invalid.</summary>
        public static Error InvalidCredentials => Error.Unauthorized(
            code: Codes.InvalidCredentials,
            message: "The supplied credentials are invalid.");

        /// <summary>The account is inactive.</summary>
        public static Error Inactive => Error.Forbidden(
            code: Codes.Inactive,
            message: "The account is inactive.");

        /// <summary>The account is locked out.</summary>
        public static Error LockedOut => Error.Unauthorized(
            code: Codes.LockedOut,
            message: "The account is locked out.");

        /// <summary>The account cannot sign in.</summary>
        public static Error NotAllowed => Error.Unauthorized(
            code: Codes.NotAllowed,
            message: "The account cannot sign in.");

        /// <summary>Email confirmation is required.</summary>
        public static Error EmailNotConfirmed => Error.Unauthorized(
            code: Codes.EmailNotConfirmed,
            message: "Email confirmation is required.");

        /// <summary>The confirmation or reset token is invalid.</summary>
        public static Error InvalidToken => Error.BadRequest(
            code: Codes.InvalidToken,
            message: "The token is invalid or has expired.");

        /// <summary>The refresh token is required.</summary>
        public static Error RefreshTokenRequired => Error.BadRequest(
            code: Codes.RefreshTokenRequired,
            message: "The refresh token is required.");

        /// <summary>The current password is required.</summary>
        public static Error CurrentPasswordRequired => Error.BadRequest(
            code: Codes.CurrentPasswordRequired,
            message: "The current password is required.");

        /// <summary>The current password is incorrect.</summary>
        public static Error CurrentPasswordInvalid => Error.BadRequest(
            code: Codes.CurrentPasswordInvalid,
            message: "The current password is incorrect.");

        /// <summary>The new password is invalid.</summary>
        public static Error PasswordInvalid(string reason) => Error.BadRequest(
            code: Codes.PasswordInvalid,
            message: reason);

        /// <summary>Two-factor authentication is required.</summary>
        public static Error TwoFactorRequired => Error.Unauthorized(
            code: Codes.TwoFactorRequired,
            message: "A two-factor verification code is required.");

        /// <summary>The two-factor code is invalid.</summary>
        public static Error TwoFactorInvalid => Error.Unauthorized(
            code: Codes.TwoFactorInvalid,
            message: "The two-factor verification code is invalid.");

        /// <summary>Two-factor authentication is not enabled.</summary>
        public static Error TwoFactorNotEnabled => Error.BadRequest(
            code: Codes.TwoFactorNotEnabled,
            message: "Two-factor authentication is not enabled.");

        /// <summary>The external provider is not configured.</summary>
        public static Error ExternalProviderUnsupported(string provider) => Error.BadRequest(
            code: Codes.ExternalProviderUnsupported,
            message: $"External provider '{provider}' is not configured.");

        /// <summary>External login failed.</summary>
        public static Error ExternalLoginFailed => Error.Unauthorized(
            code: Codes.ExternalLoginFailed,
            message: "External login failed.");

        /// <summary>Email delivery failed.</summary>
        public static Error EmailDeliveryFailed => Error.Unexpected(
            code: Codes.EmailDeliveryFailed,
            message: "The identity email could not be sent.");

        /// <summary>An Identity operation failed.</summary>
        public static Error OperationFailed(string reason) => Error.BadRequest(
            code: Codes.OperationFailed,
            message: reason);

        /// <summary>Maps an <c>IdentityResult</c> failure to a stable error.</summary>
        /// <param name="code">The Identity error code.</param>
        /// <param name="description">The Identity error description.</param>
        /// <returns>The mapped error.</returns>
        public static Error IdentityError(string code, string description)
        {
            // Enforce: duplicate identities stay 409 no matter which Identity path found them.
            if (code == "DuplicateUserName")
                return UserNameTaken;

            if (code == "DuplicateEmail")
                return EmailTaken;

            return Error.BadRequest(Codes.FromIdentityError(code), description);
        }

        /// <summary>The external sign-in could not be completed.</summary>
        public static Error ExternalSignInFailed => Error.Unauthorized(
            code: Codes.ExternalSignInFailed,
            message: "The external sign-in could not be completed.");

        /// <summary>The external provider did not return a subject identifier.</summary>
        public static Error ExternalSubjectMissing => Error.Unauthorized(
            code: Codes.ExternalSubjectMissing,
            message: "The external provider did not return a subject identifier.");

        /// <summary>The external sign-in code is invalid or expired.</summary>
        public static Error ExternalCodeInvalid => Error.Unauthorized(
            code: Codes.ExternalCodeInvalid,
            message: "The external sign-in code is invalid or expired.");

        /// <summary>The external sign-in code payload is invalid.</summary>
        public static Error ExternalCodeCorrupt => Error.Unexpected(
            code: Codes.ExternalCodeInvalid,
            message: "The external sign-in code is invalid.");

        /// <summary>The external sign-in code could not be stored.</summary>
        public static Error ExternalCodeStoreFailed => Error.Unexpected(
            code: Codes.ExternalCodeStoreFailed,
            message: "The external sign-in code could not be stored.");

        /// <summary>The requested external provider is not configured.</summary>
        public static Error ExternalProviderNotConfigured => Error.NotFound(
            code: Codes.ExternalProviderNotConfigured,
            message: "The requested external provider is not configured.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        /// <summary>Maps an ASP.NET Identity error code to a stable application code.</summary>
        /// <param name="identityCode">The code reported by Identity.</param>
        /// <returns>The stable application code.</returns>
        public static string FromIdentityError(string identityCode) => identityCode switch
        {
            "InvalidUserName" => "Identity.UserName.Invalid",
            "InvalidEmail" => "Identity.Email.Invalid",
            "PasswordTooShort" => "Identity.Password.TooShort",
            "PasswordTooLong" => "Identity.Password.TooLong",
            "PasswordRequiresDigit" => "Identity.Password.Digit",
            "PasswordRequiresLower" => "Identity.Password.Lowercase",
            "PasswordRequiresUpper" => "Identity.Password.Uppercase",
            "PasswordRequiresNonAlphanumeric" => "Identity.Password.NonAlphanumeric",
            "PasswordMismatch" => "Identity.Password.Mismatch",
            "LoginAlreadyAssociated" => "Identity.External.AlreadyLinked",
            _ => "Identity.Operation.Failed"
        };

        public const string UserNotFound = "Identity.User.NotFound";
        public const string UserRequired = "Identity.User.Required";
        public const string UserNameTaken = "Identity.UserName.Taken";
        public const string EmailTaken = "Identity.Email.Taken";
        public const string InvalidCredentials = "Identity.Credentials.Invalid";
        public const string Inactive = "Identity.User.Inactive";
        public const string LockedOut = "Identity.User.LockedOut";
        public const string NotAllowed = "Identity.User.NotAllowed";
        public const string EmailNotConfirmed = "Identity.Email.NotConfirmed";
        public const string InvalidToken = "Identity.Token.Invalid";
        public const string RefreshTokenRequired = "Identity.RefreshToken.Required";
        public const string CurrentPasswordRequired = "Identity.Password.CurrentRequired";
        public const string CurrentPasswordInvalid = "Identity.Password.CurrentInvalid";
        public const string PasswordInvalid = "Identity.Password.Invalid";
        public const string TwoFactorRequired = "Identity.TwoFactor.Required";
        public const string TwoFactorInvalid = "Identity.TwoFactor.Invalid";
        public const string TwoFactorNotEnabled = "Identity.TwoFactor.NotEnabled";
        public const string ExternalProviderUnsupported = "Identity.External.ProviderUnsupported";
        public const string ExternalLoginFailed = "Identity.External.LoginFailed";
        public const string EmailDeliveryFailed = "Identity.Email.DeliveryFailed";
        public const string OperationFailed = "Identity.Operation.Failed";
        public const string ExternalSignInFailed = "Auth.External.LoginFailed";
        public const string ExternalSubjectMissing = "Auth.External.SubjectMissing";
        public const string ExternalCodeInvalid = "Auth.External.CodeInvalid";
        public const string ExternalCodeStoreFailed = "Auth.External.CodeStoreFailed";
        public const string ExternalProviderNotConfigured = "Auth.External.ProviderUnsupported";
    }
}

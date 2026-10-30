namespace Application.Features.Identity.Auth.Outcome;

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
            code: "Identity.User.NotFound",
            message: "The specified user was not found.");

        /// <summary>The user entity is missing.</summary>
        public static Error UserRequired => Error.UnprocessableEntity(
            code: "Identity.User.Required",
            message: "An identity user is required.");

        /// <summary>The username is already registered.</summary>
        public static Error UserNameTaken => Error.Conflict(
            code: "Identity.UserName.Taken",
            message: "The username is already registered.");

        /// <summary>The email is already registered.</summary>
        public static Error EmailTaken => Error.Conflict(
            code: "Identity.Email.Taken",
            message: "The email address is already registered.");

        /// <summary>The supplied credentials are invalid.</summary>
        public static Error InvalidCredentials => Error.Unauthorized(
            code: "Identity.Credentials.Invalid",
            message: "The supplied credentials are invalid.");

        /// <summary>The account is inactive.</summary>
        public static Error Inactive => Error.Forbidden(
            code: "Identity.User.Inactive",
            message: "The account is inactive.");

        /// <summary>The account is locked out.</summary>
        public static Error LockedOut => Error.Unauthorized(
            code: "Identity.User.LockedOut",
            message: "The account is locked out.");

        /// <summary>The account cannot sign in.</summary>
        public static Error NotAllowed => Error.Unauthorized(
            code: "Identity.User.NotAllowed",
            message: "The account cannot sign in.");

        /// <summary>Email confirmation is required.</summary>
        public static Error EmailNotConfirmed => Error.Unauthorized(
            code: "Identity.Email.NotConfirmed",
            message: "Email confirmation is required.");

        /// <summary>The confirmation or reset token is invalid.</summary>
        public static Error InvalidToken => Error.BadRequest(
            code: "Identity.Token.Invalid",
            message: "The token is invalid or has expired.");

        /// <summary>The refresh token is required.</summary>
        public static Error RefreshTokenRequired => Error.BadRequest(
            code: "Identity.RefreshToken.Required",
            message: "The refresh token is required.");

        /// <summary>The current password is required.</summary>
        public static Error CurrentPasswordRequired => Error.BadRequest(
            code: "Identity.Password.CurrentRequired",
            message: "The current password is required.");

        /// <summary>The current password is incorrect.</summary>
        public static Error CurrentPasswordInvalid => Error.BadRequest(
            code: "Identity.Password.CurrentInvalid",
            message: "The current password is incorrect.");

        /// <summary>The new password is invalid.</summary>
        public static Error PasswordInvalid(string reason) => Error.BadRequest(
            code: "Identity.Password.Invalid",
            message: reason);

        /// <summary>Two-factor authentication is required.</summary>
        public static Error TwoFactorRequired => Error.Unauthorized(
            code: "Identity.TwoFactor.Required",
            message: "A two-factor verification code is required.");

        /// <summary>The two-factor code is invalid.</summary>
        public static Error TwoFactorInvalid => Error.Unauthorized(
            code: "Identity.TwoFactor.Invalid",
            message: "The two-factor verification code is invalid.");

        /// <summary>Two-factor authentication is not enabled.</summary>
        public static Error TwoFactorNotEnabled => Error.BadRequest(
            code: "Identity.TwoFactor.NotEnabled",
            message: "Two-factor authentication is not enabled.");

        /// <summary>The external provider is not configured.</summary>
        public static Error ExternalProviderUnsupported(string provider) => Error.BadRequest(
            code: "Identity.External.ProviderUnsupported",
            message: $"External provider '{provider}' is not configured.");

        /// <summary>External login failed.</summary>
        public static Error ExternalLoginFailed => Error.Unauthorized(
            code: "Identity.External.LoginFailed",
            message: "External login failed.");

        /// <summary>Email delivery failed.</summary>
        public static Error EmailDeliveryFailed => Error.Unexpected(
            code: "Identity.Email.DeliveryFailed",
            message: "The identity email could not be sent.");

        /// <summary>An Identity operation failed.</summary>
        public static Error OperationFailed(string reason) => Error.BadRequest(
            code: "Identity.Operation.Failed",
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

            string stableCode = code switch
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

            return Error.BadRequest(stableCode, description);
        }
    }
}

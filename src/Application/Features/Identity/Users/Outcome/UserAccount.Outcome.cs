namespace Application.Features.Identity.Users.Outcome;

/// <summary>Failures produced by the admin user account use cases.</summary>
public static class UserAccountOutcome
{
    /// <summary>Failure errors for user account operations.</summary>
    public static class Failure
    {
        /// <summary>The requested user does not exist.</summary>
        public static Error UserNotFound => Error.NotFound(
            code: "Identity.User.NotFound",
            message: "The specified user was not found.");

        /// <summary>The requested role does not exist.</summary>
        public static Error RoleNotFound => Error.NotFound(
            code: "Identity.Role.NotFound",
            message: "The specified role was not found.");

        /// <summary>The username is missing or outside the allowed length.</summary>
        public static Error UserNameInvalid => Error.BadRequest(
            code: "Identity.UserName.Invalid",
            message: "The username is missing or has an invalid length.");

        /// <summary>The email address is missing or malformed.</summary>
        public static Error EmailInvalid => Error.BadRequest(
            code: "Identity.Email.Invalid",
            message: "The email address is missing or invalid.");

        /// <summary>The role identifier is empty.</summary>
        public static Error RoleInvalid => Error.BadRequest(
            code: "Identity.Role.Invalid",
            message: "The role identifier is invalid.");

        /// <summary>The replacement password violates the application policy.</summary>
        /// <param name="reason">The human-readable reason.</param>
        /// <returns>The password failure.</returns>
        public static Error PasswordInvalid(string reason) => Error.BadRequest(
            code: "Identity.Password.Invalid",
            message: reason);
    }
}

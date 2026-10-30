namespace Application.Features.Identity.Users.Outcome;

/// <summary>Failures specific to the admin user account use cases.</summary>
/// <remarks>
/// Errors shared with the auth slice (user not found, password policy) come from
/// <c>IdentityOutcome</c>; this type only owns failures that are unique to admin
/// user management.
/// </remarks>
public static class UserAccountOutcome
{
    /// <summary>Failure errors for user account operations.</summary>
    public static class Failure
    {
        /// <summary>The requested role does not exist.</summary>
        public static Error RoleNotFound => Error.NotFound(
            code: "Identity.Role.NotFound",
            message: "The specified role was not found.");

        /// <summary>The email address is missing or malformed.</summary>
        public static Error EmailInvalid => Error.BadRequest(
            code: "Identity.Email.Invalid",
            message: "The email address is missing or invalid.");

        /// <summary>The role identifier is empty.</summary>
        public static Error RoleInvalid => Error.BadRequest(
            code: "Identity.Role.Invalid",
            message: "The role identifier is invalid.");

        /// <summary>The replacement first name exceeds the allowed length.</summary>
        public static Error FirstNameTooLong => Error.BadRequest(
            code: "Identity.User.FirstName.TooLong",
            message: "The first name is too long.");

        /// <summary>The replacement last name exceeds the allowed length.</summary>
        public static Error LastNameTooLong => Error.BadRequest(
            code: "Identity.User.LastName.TooLong",
            message: "The last name is too long.");
    }
}

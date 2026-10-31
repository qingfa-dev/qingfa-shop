using Application.BuildingBlocks.Querying;

namespace Application.BuildingBlocks.Security.Identity.Users.Outcome;

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
            code: Codes.RoleNotFound,
            message: "The specified role was not found.");

        /// <summary>The email address is missing or malformed.</summary>
        public static Error EmailInvalid => Error.BadRequest(
            code: Codes.EmailInvalid,
            message: "The email address is missing or invalid.");

        /// <summary>The role identifier is empty.</summary>
        public static Error RoleInvalid => Error.BadRequest(
            code: Codes.RoleInvalid,
            message: "The role identifier is invalid.");

        /// <summary>The replacement first name exceeds the allowed length.</summary>
        public static Error FirstNameTooLong => Error.BadRequest(
            code: Codes.FirstNameTooLong,
            message: "The first name is too long.");

        /// <summary>The replacement last name exceeds the allowed length.</summary>
        public static Error LastNameTooLong => Error.BadRequest(
            code: Codes.LastNameTooLong,
            message: "The last name is too long.");

        /// <summary>A search parameter is not supported for user search.</summary>
        public static Error UnsupportedSearch(string parameter) => Error.BadRequest(
            code: Codes.UnsupportedSearch,
            message: $"{parameter} is not supported for user search.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string RoleNotFound = "Identity.Role.NotFound";
        public const string EmailInvalid = "Identity.Email.Invalid";
        public const string RoleInvalid = "Identity.Role.Invalid";
        public const string FirstNameTooLong = "Identity.User.FirstName.TooLong";
        public const string LastNameTooLong = "Identity.User.LastName.TooLong";
        public const string UnsupportedSearch = QueryOutcome.Codes.Unsupported;
    }
}

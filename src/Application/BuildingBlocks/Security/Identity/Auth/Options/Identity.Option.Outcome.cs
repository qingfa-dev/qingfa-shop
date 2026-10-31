namespace Application.BuildingBlocks.Security.Identity.Auth.Options;

/// <summary>Validation failures and stable codes for <see cref="IdentityApiOption"/>.</summary>
public static class IdentityOptionOutcome
{
    /// <summary>Failure errors raised when the identity API options are invalid.</summary>
    public static class Failure
    {
        /// <summary>The default role is missing.</summary>
        public static Error DefaultRoleRequired => Error.Validation(
            code: Codes.DefaultRoleRequired,
            message: "IdentityApi:DefaultRole is required.");

        /// <summary>The public base URL is not absolute.</summary>
        public static Error PublicBaseUrlInvalid => Error.Validation(
            code: Codes.PublicBaseUrlInvalid,
            message: "IdentityApi:PublicBaseUrl must be an absolute URL.");

        /// <summary>The email confirmation path is missing.</summary>
        public static Error ConfirmationPathRequired => Error.Validation(
            code: Codes.ConfirmationPathRequired,
            message: "IdentityApi:ConfirmationPath is required.");

        /// <summary>The password reset path is missing.</summary>
        public static Error ResetPasswordPathRequired => Error.Validation(
            code: Codes.ResetPasswordPathRequired,
            message: "IdentityApi:ResetPasswordPath is required.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string DefaultRoleRequired = "Identity.DefaultRole.Required";
        public const string PublicBaseUrlInvalid = "Identity.PublicBaseUrl.Invalid";
        public const string ConfirmationPathRequired = "Identity.ConfirmationPath.Required";
        public const string ResetPasswordPathRequired = "Identity.ResetPasswordPath.Required";
    }
}

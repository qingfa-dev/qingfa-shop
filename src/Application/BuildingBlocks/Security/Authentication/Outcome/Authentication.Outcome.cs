namespace Application.BuildingBlocks.Security.Authentication.Outcome;

/// <summary>Stable failures for authentication challenges and authorization gates.</summary>
public static class AuthenticationOutcome
{
    /// <summary>Failure factories.</summary>
    public static class Failure
    {
        /// <summary>The request is not authenticated.</summary>
        public static Error Required => Error.Unauthorized(
            Codes.Required,
            "Authentication is required.");

        /// <summary>The caller is authenticated but not allowed to perform the operation.</summary>
        public static Error Forbidden => Error.Forbidden(
            Codes.Forbidden,
            "The caller is not allowed to perform this operation.");

        /// <summary>The authenticated user identifier is missing or invalid.</summary>
        public static Error UserIdMissing => Error.Unauthorized(
            Codes.UserIdMissing,
            "The authenticated user identifier is missing or invalid.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string Required = "Authentication.Required";
        public const string Forbidden = "Authorization.Forbidden";
        public const string UserIdMissing = "Authentication.UserIdMissing";
    }
}

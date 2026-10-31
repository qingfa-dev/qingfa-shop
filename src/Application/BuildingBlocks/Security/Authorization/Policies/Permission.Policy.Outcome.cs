namespace Application.BuildingBlocks.Security.Authorization.Policies;

/// <summary>Stable failures for permission-policy composition.</summary>
public static class PermissionPolicyOutcome
{
    /// <summary>Failure factories.</summary>
    public static class Failure
    {
        /// <summary>No permission identifier was supplied.</summary>
        public static Error NoPermissions => Error.Validation(
            Codes.NoPermissions,
            "At least one permission identifier is required.");

        /// <summary>One identifier is not canonical.</summary>
        /// <param name="identifier">The invalid identifier.</param>
        /// <returns>A validation error naming the identifier.</returns>
        public static Error InvalidIdentifier(string identifier) => Error.Validation(
            Codes.InvalidIdentifier,
            $"'{identifier}' is not a canonical permission identifier.");
    }

    /// <summary>Stable error codes.</summary>
    public static class Codes
    {
        public const string NoPermissions = "Permission.Policy.NoPermissions";
        public const string InvalidIdentifier = "Permission.Policy.InvalidIdentifier";
    }
}

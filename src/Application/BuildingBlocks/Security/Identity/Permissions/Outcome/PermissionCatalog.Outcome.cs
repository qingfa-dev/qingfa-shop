using Application.BuildingBlocks.Querying;

namespace Application.BuildingBlocks.Security.Identity.Permissions.Outcome;

/// <summary>Failures produced by the permission catalog service.</summary>
public static class PermissionCatalogOutcome
{
    /// <summary>Failure errors for catalog operations.</summary>
    public static class Failure
    {
        /// <summary>The requested permission is not in the catalog.</summary>
        /// <param name="identifier">The missing identifier.</param>
        /// <returns>The not-found error.</returns>
        public static Error NotFound(string identifier) =>
            Error.NotFound(
                code: Codes.NotFound,
                message: $"Permission '{identifier}' was not found in the catalog.");

        /// <summary>The caller supplied no permission identifiers to check.</summary>
        public static Error PermissionsRequired => Error.BadRequest(
            code: Codes.PermissionsRequired,
            message: "At least one permission identifier is required.");

        /// <summary>A search parameter is not supported for permission search.</summary>
        public static Error UnsupportedSearch(string parameter) => Error.BadRequest(
            code: Codes.UnsupportedSearch,
            message: $"{parameter} is not supported for permission search.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string NotFound = "PermissionCatalog.NotFound";
        public const string PermissionsRequired = "PermissionCatalog.Permissions.Required";
        public const string UnsupportedSearch = QueryOutcome.Codes.Unsupported;
    }
}

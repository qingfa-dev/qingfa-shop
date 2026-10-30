namespace Application.Features.Identity.Permissions.Outcome;

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
                code: "PermissionCatalog.NotFound",
                message: $"Permission '{identifier}' was not found in the catalog.");

        /// <summary>The caller supplied no permission identifiers to check.</summary>
        public static Error PermissionsRequired => Error.BadRequest(
            code: "PermissionCatalog.Permissions.Required",
            message: "At least one permission identifier is required.");
    }
}

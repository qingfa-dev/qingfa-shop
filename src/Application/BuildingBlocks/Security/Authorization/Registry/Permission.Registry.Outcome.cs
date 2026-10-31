namespace Application.BuildingBlocks.Security.Authorization.Registry;

/// <summary>Failures produced by the permission metadata registry.</summary>
public static class PermissionRegistryOutcome
{
    /// <summary>Failure errors for registry refresh operations.</summary>
    public static class Failure
    {
        /// <summary>The registry could not rebuild its snapshot.</summary>
        /// <param name="exception">The underlying failure, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error RefreshFailed(Exception? exception = null) => Error.ServiceUnavailable(
            code: Codes.RefreshFailed,
            message: exception is null
                ? "Permission metadata could not be refreshed."
                : $"Permission metadata could not be refreshed ({exception.GetType().Name}).");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string RefreshFailed = "PermissionRegistry.RefreshFailed";
    }
}

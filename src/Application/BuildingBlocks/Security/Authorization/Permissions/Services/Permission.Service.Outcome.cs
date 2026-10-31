namespace Application.BuildingBlocks.Security.Authorization.Permissions.Services;

/// <summary>Failures produced by permission orchestration.</summary>
/// <remarks>
/// Cache plumbing failures are raised by <c>PermissionCacheOutcome</c>; this outcome adds
/// the service-level context needed to decide whether a mutation or invalidation is safe.
/// </remarks>
public static class PermissionServiceOutcome
{
    /// <summary>Failure errors for permission service operations.</summary>
    public static class Failure
    {
        /// <summary>The caller supplied an empty user identifier.</summary>
        public static Error UserIdRequired => Error.BadRequest(
            code: Codes.UserIdRequired,
            message: "A user identifier is required.");

        /// <summary>The caller supplied an empty role identifier.</summary>
        public static Error RoleIdRequired => Error.BadRequest(
            code: Codes.RoleIdRequired,
            message: "A role identifier is required.");

        /// <summary>The caller supplied no permission collection.</summary>
        public static Error PermissionsRequired => Error.BadRequest(
            code: Codes.PermissionsRequired,
            message: "A permission collection is required.");

        /// <summary>The role change succeeded, but cached permissions could not be invalidated.</summary>
        /// <param name="roleId">The affected role.</param>
        /// <param name="causes">The underlying cache failures.</param>
        /// <returns>The availability error.</returns>
        public static Error RoleInvalidationFailed(Guid roleId, IEnumerable<Error> causes) =>
            Error.ServiceUnavailable(
                code: Codes.RoleInvalidationFailed,
                message: $"Role '{roleId}' changed but its cached permissions could not be invalidated. {Describe(causes)}");

        /// <summary>The user change succeeded, but cached permissions could not be invalidated.</summary>
        /// <param name="userId">The affected user.</param>
        /// <param name="causes">The underlying cache failures.</param>
        /// <returns>The availability error.</returns>
        public static Error UserInvalidationFailed(Guid userId, IEnumerable<Error> causes) =>
            Error.ServiceUnavailable(
                code: Codes.UserInvalidationFailed,
                message: $"User '{userId}' changed but their cached permissions could not be invalidated. {Describe(causes)}");

        private static string Describe(IEnumerable<Error> causes)
        {
            string detail = string.Join(
                "; ",
                causes.Select(cause => $"{cause.Code}: {cause.Message}"));

            return string.IsNullOrWhiteSpace(detail) ? "No cause was reported." : detail;
        }
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string UserIdRequired = "PermissionService.UserId.Required";
        public const string RoleIdRequired = "PermissionService.RoleId.Required";
        public const string PermissionsRequired = "PermissionService.Permissions.Required";
        public const string RoleInvalidationFailed = "PermissionService.Role.InvalidationFailed";
        public const string UserInvalidationFailed = "PermissionService.User.InvalidationFailed";
    }
}

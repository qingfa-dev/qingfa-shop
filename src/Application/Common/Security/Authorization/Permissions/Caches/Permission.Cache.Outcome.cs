namespace Application.Common.Security.Authorization.Permissions.Caches;

/// <summary>Failures produced by the permission cache.</summary>
/// <remarks>
/// The cache port reports reusable plumbing failures through <c>CachingOutcome</c>. This
/// outcome follows the same template but adds permission-specific context (user, role,
/// operation) so callers can react without parsing messages.
/// </remarks>
public static class PermissionCacheOutcome
{
    /// <summary>Failure errors for permission cache operations.</summary>
    public static class Failure
    {
        /// <summary>The caller supplied an empty user identifier.</summary>
        public static Error UserIdRequired => Error.BadRequest(
            code: "PermissionCache.UserId.Required",
            message: "A user identifier is required.");

        /// <summary>The caller supplied an empty role identifier.</summary>
        public static Error RoleIdRequired => Error.BadRequest(
            code: "PermissionCache.RoleId.Required",
            message: "A role identifier is required.");

        /// <summary>The caller supplied no permission set to cache.</summary>
        public static Error PermissionsRequired => Error.BadRequest(
            code: "PermissionCache.Permissions.Required",
            message: "A permission set is required.");

        /// <summary>The underlying cache failed while reading user permissions.</summary>
        /// <param name="userId">The affected user.</param>
        /// <param name="causes">The underlying cache failures.</param>
        /// <returns>The availability error.</returns>
        public static Error UserReadFailed(Guid userId, IEnumerable<Error> causes) =>
            Error.ServiceUnavailable(
                code: "PermissionCache.User.ReadFailed",
                message: $"Failed to read cached permissions for user '{userId}'. {Describe(causes)}");

        /// <summary>The underlying cache failed while reading role permissions.</summary>
        /// <param name="roleId">The affected role.</param>
        /// <param name="causes">The underlying cache failures.</param>
        /// <returns>The availability error.</returns>
        public static Error RoleReadFailed(Guid roleId, IEnumerable<Error> causes) =>
            Error.ServiceUnavailable(
                code: "PermissionCache.Role.ReadFailed",
                message: $"Failed to read cached permissions for role '{roleId}'. {Describe(causes)}");

        /// <summary>The underlying cache failed while storing user permissions.</summary>
        /// <param name="userId">The affected user.</param>
        /// <param name="causes">The underlying cache failures.</param>
        /// <returns>The availability error.</returns>
        public static Error UserWriteFailed(Guid userId, IEnumerable<Error> causes) =>
            Error.ServiceUnavailable(
                code: "PermissionCache.User.WriteFailed",
                message: $"Failed to cache permissions for user '{userId}'. {Describe(causes)}");

        /// <summary>The underlying cache failed while storing role permissions.</summary>
        /// <param name="roleId">The affected role.</param>
        /// <param name="causes">The underlying cache failures.</param>
        /// <returns>The availability error.</returns>
        public static Error RoleWriteFailed(Guid roleId, IEnumerable<Error> causes) =>
            Error.ServiceUnavailable(
                code: "PermissionCache.Role.WriteFailed",
                message: $"Failed to cache permissions for role '{roleId}'. {Describe(causes)}");

        /// <summary>The underlying cache failed while invalidating a user.</summary>
        /// <param name="userId">The affected user.</param>
        /// <param name="causes">The underlying cache failures.</param>
        /// <returns>The availability error.</returns>
        public static Error UserInvalidationFailed(Guid userId, IEnumerable<Error> causes) =>
            Error.ServiceUnavailable(
                code: "PermissionCache.User.InvalidationFailed",
                message: $"Failed to invalidate cached permissions for user '{userId}'. {Describe(causes)}");

        /// <summary>The underlying cache failed while invalidating a role.</summary>
        /// <param name="roleId">The affected role.</param>
        /// <param name="causes">The underlying cache failures.</param>
        /// <returns>The availability error.</returns>
        public static Error RoleInvalidationFailed(Guid roleId, IEnumerable<Error> causes) =>
            Error.ServiceUnavailable(
                code: "PermissionCache.Role.InvalidationFailed",
                message: $"Failed to invalidate cached permissions for role '{roleId}'. {Describe(causes)}");

        /// <summary>The underlying cache failed while invalidating every permission entry.</summary>
        /// <param name="causes">The underlying cache failures.</param>
        /// <returns>The availability error.</returns>
        public static Error AllInvalidationFailed(IEnumerable<Error> causes) =>
            Error.ServiceUnavailable(
                code: "PermissionCache.All.InvalidationFailed",
                message: $"Failed to invalidate every cached permission entry. {Describe(causes)}");

        private static string Describe(IEnumerable<Error> causes)
        {
            string detail = string.Join(
                "; ",
                causes.Select(cause => $"{cause.Code}: {cause.Message}"));

            return string.IsNullOrWhiteSpace(detail) ? "No cause was reported." : detail;
        }
    }
}

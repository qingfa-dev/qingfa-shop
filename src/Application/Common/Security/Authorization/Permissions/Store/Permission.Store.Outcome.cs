namespace Application.Common.Security.Authorization.Permissions.Store;

/// <summary>Failures produced by the permission store.</summary>
/// <remarks>
/// Read failures are availability errors because the store intentionally degrades to an
/// empty permission set. Write failures are reported per operation so callers can log and
/// retry the exact mutation without guessing from a shared code.
/// </remarks>
public static class PermissionStoreOutcome
{
    /// <summary>Failure errors for permission store operations.</summary>
    public static class Failure
    {
        /// <summary>The requested identifier is not a canonical permission identifier.</summary>
        /// <param name="identifier">The rejected identifier.</param>
        /// <returns>The validation error.</returns>
        public static Error InvalidIdentifier(string? identifier) => Error.Validation(
            code: "PermissionStore.Identifier.Invalid",
            message: $"'{identifier}' is not a canonical permission identifier.");

        /// <summary>The requested batch exceeds the configured limit.</summary>
        /// <param name="count">The requested item count.</param>
        /// <returns>The validation error.</returns>
        public static Error BatchTooLarge(int count) => Error.Validation(
            code: "PermissionStore.Batch.TooLarge",
            message: $"A permission batch cannot exceed {PermissionStoreConstant.Constraints.MaxPermissionsPerBatch} items. (Provided: {count})");

        /// <summary>The merged user permission read failed.</summary>
        /// <param name="userId">The affected user.</param>
        /// <param name="exception">The underlying persistence exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error UserPermissionsReadFailed(Guid userId, Exception? exception = null) =>
            ReadFailed("User.Permissions", $"user '{userId}'", exception);

        /// <summary>The role permission read failed.</summary>
        /// <param name="roleId">The affected role.</param>
        /// <param name="exception">The underlying persistence exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error RolePermissionsReadFailed(Guid roleId, Exception? exception = null) =>
            ReadFailed("Role.Permissions", $"role '{roleId}'", exception);

        /// <summary>The user role-id read failed.</summary>
        /// <param name="userId">The affected user.</param>
        /// <param name="exception">The underlying persistence exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error UserRoleIdsReadFailed(Guid userId, Exception? exception = null) =>
            ReadFailed("User.RoleIds", $"user '{userId}'", exception);

        /// <summary>The direct user permission read failed.</summary>
        /// <param name="userId">The affected user.</param>
        /// <param name="exception">The underlying persistence exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error UserDirectPermissionsReadFailed(
            Guid userId,
            Exception? exception = null) =>
            ReadFailed("User.DirectPermissions", $"user '{userId}'", exception);

        /// <summary>The catalog-wide identifier read failed.</summary>
        /// <param name="exception">The underlying persistence exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error AllIdentifiersReadFailed(Exception? exception = null) =>
            ReadFailed("AllIdentifiers", "the permission catalog", exception);

        /// <summary>The role permission batch write failed.</summary>
        /// <param name="roleId">The affected role.</param>
        /// <param name="exception">The underlying persistence exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error RolePermissionsAddFailed(Guid roleId, Exception? exception = null) =>
            WriteFailed("Role.Permissions.Add", $"role '{roleId}'", exception);

        /// <summary>The role permission batch removal failed.</summary>
        /// <param name="roleId">The affected role.</param>
        /// <param name="exception">The underlying persistence exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error RolePermissionsRemoveFailed(Guid roleId, Exception? exception = null) =>
            WriteFailed("Role.Permissions.Remove", $"role '{roleId}'", exception);

        /// <summary>The direct user permission batch write failed.</summary>
        /// <param name="userId">The affected user.</param>
        /// <param name="exception">The underlying persistence exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error UserPermissionsAddFailed(Guid userId, Exception? exception = null) =>
            WriteFailed("User.Permissions.Add", $"user '{userId}'", exception);

        /// <summary>The direct user permission batch removal failed.</summary>
        /// <param name="userId">The affected user.</param>
        /// <param name="exception">The underlying persistence exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error UserPermissionsRemoveFailed(Guid userId, Exception? exception = null) =>
            WriteFailed("User.Permissions.Remove", $"user '{userId}'", exception);

        private static Error ReadFailed(
            string operation,
            string subject,
            Exception? exception) => Error.ServiceUnavailable(
            code: $"PermissionStore.{operation}.ReadFailed",
            message: $"Failed to read permissions for {subject}.{Describe(exception)}");

        private static Error WriteFailed(
            string operation,
            string subject,
            Exception? exception) => Error.ServiceUnavailable(
            code: $"PermissionStore.{operation}.Failed",
            message: $"Failed to persist permissions for {subject}.{Describe(exception)}");

        private static string Describe(Exception? exception) =>
            exception is null ? string.Empty : $" ({exception.GetType().Name})";
    }
}

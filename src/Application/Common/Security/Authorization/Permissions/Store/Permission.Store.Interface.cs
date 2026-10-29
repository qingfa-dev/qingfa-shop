namespace Application.Common.Security.Authorization.Permissions.Store;

// Boundary: Store → Persistence — Single source of truth for permission claims; results cached by IPermissionCache.

/// <summary>
/// Reads the effective permission set for a user by merging:
///   (1) Claims on every role the user belongs to  (AspNetRoleClaims)
///   (2) Claims assigned directly on the user       (AspNetUserClaims)
///
/// This is the single source of truth for permissions. The result is
/// cached per-user by IPermissionCache so the DB is not hit on every request.
/// </summary>
public interface IPermissionStore
{
    /// <summary>
    /// Retrieves the combined set of permissions for a user from role claims and direct user claims.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the set of permission strings.</returns>
    Task<Result<HashSet<string>>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the set of permissions assigned to a specific role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the set of permission strings for the role.</returns>
    Task<Result<HashSet<string>>> GetRolePermissionsAsync(
        Guid roleId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the set of role IDs assigned to a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the set of role IDs for the user.</returns>
    Task<Result<HashSet<Guid>>> GetUserRoleIdsAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the set of direct permissions assigned to a user (excluding role claims).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the set of direct permission strings for the user.</returns>
    Task<Result<HashSet<string>>> GetUserDirectPermissionsAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a list of permissions to a role in a single batch operation.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="permissions">The list of permission strings to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AddRolePermissionsAsync(Guid roleId, IEnumerable<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Removes a list of permissions from a role in a single batch operation.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="permissions">The list of permission strings to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> RemoveRolePermissionsAsync(Guid roleId, IEnumerable<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Adds a list of direct permissions to a user in a single batch operation.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="permissions">The list of permission strings to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AddUserDirectPermissionsAsync(Guid userId, IEnumerable<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Removes a list of direct permissions from a user in a single batch operation.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="permissions">The list of permission strings to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> RemoveUserDirectPermissionsAsync(Guid userId, IEnumerable<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Loads all distinct permission identifiers from role claims and user claims across the entire system.
    /// Used by the permission registry to discover runtime-assigned permissions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the set of all distinct permission identifier strings.</returns>
    Task<Result<HashSet<string>>> GetAllPermissionIdentifiersAsync(CancellationToken ct = default);
}

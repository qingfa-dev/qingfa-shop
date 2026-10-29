using Application.Common.Security.Authorization.Permissions.Caches;
using Application.Common.Security.Authorization.Permissions.Store;

namespace Application.Common.Security.Authorization.Permissions.Services;

/// <summary>
/// Central orchestrator for all permission-related operations.
/// Wraps and coordinates <see cref="IPermissionCache"/> and <see cref="IPermissionStore"/>.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Retrieves the effective, merged set of permissions for a user (direct claims + role claims),
    /// utilizing the tiered caching system (User Cache -> Role Cache -> Database).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the set of permission strings.</returns>
    Task<Result<HashSet<string>>> GetEffectiveUserPermissionsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the set of permissions assigned to a specific role, utilizing the role-level cache.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the set of permission strings for the role.</returns>
    Task<Result<HashSet<string>>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Verifies if a user possesses EVERY permission in the specified collection.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to check.</param>
    /// <param name="permissions">The collection of permissions to verify.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating true if all permissions are held, otherwise false.</returns>
    Task<Result<bool>> HasAllPermissionsAsync(Guid userId, IEnumerable<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Verifies if a user possesses ANY of the specified permissions.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to check.</param>
    /// <param name="permissions">The collection of permissions to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating true if at least one permission is held, otherwise false.</returns>
    Task<Result<bool>> HasAnyPermissionsAsync(Guid userId, IEnumerable<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Verifies if a role possesses EVERY permission in the specified collection.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to check.</param>
    /// <param name="permissions">The collection of permissions to verify.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating true if all permissions are held, otherwise false.</returns>
    Task<Result<bool>> RoleHasAllPermissionsAsync(Guid roleId, IEnumerable<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Verifies if a role possesses ANY of the specified permissions.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to check.</param>
    /// <param name="permissions">The collection of permissions to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating true if at least one permission is held, otherwise false.</returns>
    Task<Result<bool>> RoleHasAnyPermissionsAsync(Guid roleId, IEnumerable<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cache for a specific role and automatically purges the cache of all users associated with that role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to invalidate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> InvalidateRolePermissionsAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cache for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to invalidate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> InvalidateUserPermissionsAsync(Guid userId, CancellationToken ct = default);

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
}

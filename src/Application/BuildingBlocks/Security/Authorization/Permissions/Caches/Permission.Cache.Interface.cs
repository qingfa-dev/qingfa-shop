namespace Application.BuildingBlocks.Security.Authorization.Permissions.Caches;

// Boundary: Caching → Domain — Wraps ICacheService with permission-domain semantics (key prefixes, role tags, configurable TTL).

/// <summary>
/// Caches per-user and per-role permission sets to prevent a database round-trip
/// on every request.
///
/// Cache lifetime:    Sliding 5-minute local TTL, 30-minute absolute TTL (configurable).
/// Invalidation:      Explicit calls after any role/claim change, so permission
///                    changes take effect on the next request instead of the next login.
/// </summary>
public interface IPermissionCache
{
    /// <summary>
    /// Retrieves cached permissions for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a hit or miss entry.</returns>
    Task<Result<PermissionCacheEntry>> GetAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Stores permissions in the cache for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="permissions">The set of permission strings to cache.</param>
    /// <param name="roleIds">Optional set of role IDs assigned to the user for role-based invalidation tagging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetUserAsync(
        Guid userId,
        HashSet<string> permissions,
        IEnumerable<Guid>? roleIds = null,
        CancellationToken ct = default);

    /// <summary>
    /// Invalidates cached permissions for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> InvalidateUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Global invalidation: purges all cached permissions across all users.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> InvalidateAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a set of permissions for a role from the cache.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a hit or miss entry.</returns>
    Task<Result<PermissionCacheEntry>> GetRoleAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Stores a set of permissions for a role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="permissions">The set of permissions to cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetRoleAsync(Guid roleId, HashSet<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Invalidates a role entry and every user entry tagged with that role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to invalidate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> InvalidateRoleAsync(Guid roleId, CancellationToken ct = default);
}

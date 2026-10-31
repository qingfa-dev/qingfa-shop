using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Security.Authorization;
using Application.BuildingBlocks.Security.Identity.Permissions.Models;

namespace Application.BuildingBlocks.Security.Identity.Abstractions;

/// <summary>Admin-facing port for reading and refreshing the permission catalog.</summary>
/// <remarks>
/// The catalog is derived from the authorization metadata registry; this port
/// exposes it to the identity management routes without leaking the registry
/// implementation. Every operation returns a <see cref="Result{TValue}"/>.
/// </remarks>
public interface IPermissionCatalogService
{
    /// <summary>Searches the catalog using the shared query pipeline.</summary>
    /// <param name="parameters">The filter, ordering, search, and paging inputs.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A snapshot of the matching permissions, or a failure.</returns>
    Task<Result<PermissionSnapshotModel>> SearchAsync(
        QueryParameters parameters,
        CancellationToken ct = default);

    /// <summary>Gets a single permission by identifier.</summary>
    /// <param name="identifier">The permission identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The permission, or a failure when it does not exist.</returns>
    Task<Result<PermissionItemModel>> GetAsync(
        string identifier,
        CancellationToken ct = default);

    /// <summary>Lists the distinct permission domains.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The sorted domain names, or a failure.</returns>
    Task<Result<IReadOnlyList<string>>> ListDomainsAsync(CancellationToken ct = default);

    /// <summary>Lists the distinct permission categories.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The sorted category names, or a failure.</returns>
    Task<Result<IReadOnlyList<string>>> ListCategoriesAsync(CancellationToken ct = default);

    /// <summary>Gets the effective permissions held by a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The permission identifiers, or a failure.</returns>
    Task<Result<IReadOnlyList<string>>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>Gets the permissions assigned to a role.</summary>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The permission identifiers, or a failure.</returns>
    Task<Result<IReadOnlyList<string>>> GetRolePermissionsAsync(
        Guid roleId,
        CancellationToken ct = default);

    /// <summary>Checks whether a user holds the requested permissions.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="permissions">The permission identifiers to check.</param>
    /// <param name="mode">Whether every or any permission must be held.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when the requirement is met, or a failure.</returns>
    Task<Result<bool>> CheckUserPermissionsAsync(
        Guid userId,
        IReadOnlyList<string> permissions,
        PermissionMatchMode mode,
        CancellationToken ct = default);

    /// <summary>Rebuilds the catalog from the current authorization metadata.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The refreshed snapshot, or a failure.</returns>
    Task<Result<PermissionSnapshotModel>> RefreshAsync(CancellationToken ct = default);
}

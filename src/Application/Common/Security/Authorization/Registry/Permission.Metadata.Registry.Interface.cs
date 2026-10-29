using Application.Common.Security.Authorization.Domain;

namespace Application.Common.Security.Authorization.Registry;

/// <summary>
/// Thread-safe source of truth for permission metadata: built-in catalog entries plus
/// identifiers discovered from persisted role and user claims.
/// </summary>
public interface IPermissionMetadataRegistry
{
    /// <summary>Gets the current immutable snapshot without touching the store.</summary>
    PermissionMetadataSnapshot Snapshot { get; }

    /// <summary>Finds a permission by identifier in the current snapshot.</summary>
    /// <param name="identifier">The candidate identifier.</param>
    /// <returns>The permission metadata, or <c>null</c> when it is not registered.</returns>
    PermissionMetadata? Find(string? identifier);

    /// <summary>Returns every permission in a domain.</summary>
    /// <param name="domain">The domain name.</param>
    /// <returns>The matching permissions, or an empty list.</returns>
    IReadOnlyList<PermissionMetadata> ByDomain(string? domain);

    /// <summary>Returns every permission in a category.</summary>
    /// <param name="category">The category name.</param>
    /// <returns>The matching permissions, or an empty list.</returns>
    IReadOnlyList<PermissionMetadata> ByCategory(string? category);

    /// <summary>
    /// Rebuilds the catalog from the built-in registry and persisted permission claims,
    /// then swaps the snapshot atomically. Concurrent callers are serialized.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new snapshot, or the store failure.</returns>
    Task<Result<PermissionMetadataSnapshot>> RefreshAsync(CancellationToken ct = default);
}

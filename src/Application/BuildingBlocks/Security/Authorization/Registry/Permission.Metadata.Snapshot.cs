using System.Collections.Frozen;

using Application.BuildingBlocks.Security.Authorization.Domain;

namespace Application.BuildingBlocks.Security.Authorization.Registry;

/// <summary>Immutable, thread-safe view of the permission metadata catalog.</summary>
/// <remarks>
/// A snapshot is replaced atomically by the registry, so readers never observe a
/// partially rebuilt catalog and never need a lock.
/// </remarks>
public sealed class PermissionMetadataSnapshot
{
    private readonly FrozenDictionary<string, PermissionMetadata> _byIdentifier;
    private readonly FrozenDictionary<string, IReadOnlyList<PermissionMetadata>> _byDomain;
    private readonly FrozenDictionary<string, IReadOnlyList<PermissionMetadata>> _byCategory;

    private PermissionMetadataSnapshot(
        IReadOnlyList<PermissionMetadata> permissions,
        DateTimeOffset refreshedAtUtc)
    {
        Permissions = permissions;
        RefreshedAtUtc = refreshedAtUtc;

        _byIdentifier = permissions.ToFrozenDictionary(
            permission => permission.Identifier,
            StringComparer.OrdinalIgnoreCase);

        _byDomain = permissions
            .GroupBy(permission => permission.Domain, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                group => group.Key,
                group => (IReadOnlyList<PermissionMetadata>)[.. group],
                StringComparer.OrdinalIgnoreCase);

        _byCategory = permissions
            .GroupBy(permission => permission.Category, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                group => group.Key,
                group => (IReadOnlyList<PermissionMetadata>)[.. group],
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets an empty snapshot.</summary>
    public static PermissionMetadataSnapshot Empty { get; } =
        Create([], DateTimeOffset.MinValue);

    /// <summary>Gets every permission in the snapshot, ordered by identifier.</summary>
    public IReadOnlyList<PermissionMetadata> Permissions { get; }

    /// <summary>Gets the UTC time when the snapshot was built.</summary>
    public DateTimeOffset RefreshedAtUtc { get; }

    /// <summary>Gets the number of permissions in the snapshot.</summary>
    public int Count => Permissions.Count;

    /// <summary>Creates an immutable snapshot from the supplied permissions.</summary>
    /// <param name="permissions">The permission metadata to include.</param>
    /// <param name="refreshedAtUtc">The build timestamp.</param>
    /// <returns>The immutable snapshot.</returns>
    public static PermissionMetadataSnapshot Create(
        IEnumerable<PermissionMetadata> permissions,
        DateTimeOffset refreshedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        PermissionMetadata[] ordered = [.. permissions
            .Where(permission => permission is not null)
            .DistinctBy(permission => permission.Identifier, StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission.Identifier, StringComparer.OrdinalIgnoreCase)];

        return new PermissionMetadataSnapshot(ordered, refreshedAtUtc);
    }

    /// <summary>Finds a permission by identifier.</summary>
    /// <param name="identifier">The candidate identifier.</param>
    /// <returns>The permission metadata, or <c>null</c> when it is not registered.</returns>
    public PermissionMetadata? Find(string? identifier) =>
        !string.IsNullOrWhiteSpace(identifier) &&
        _byIdentifier.TryGetValue(identifier, out PermissionMetadata? permission)
            ? permission
            : null;

    /// <summary>Returns every permission in a domain.</summary>
    /// <param name="domain">The domain name.</param>
    /// <returns>The matching permissions, or an empty list.</returns>
    public IReadOnlyList<PermissionMetadata> ByDomain(string? domain) =>
        !string.IsNullOrWhiteSpace(domain) &&
        _byDomain.TryGetValue(domain, out IReadOnlyList<PermissionMetadata>? permissions)
            ? permissions
            : [];

    /// <summary>Returns every permission in a category.</summary>
    /// <param name="category">The category name.</param>
    /// <returns>The matching permissions, or an empty list.</returns>
    public IReadOnlyList<PermissionMetadata> ByCategory(string? category) =>
        !string.IsNullOrWhiteSpace(category) &&
        _byCategory.TryGetValue(category, out IReadOnlyList<PermissionMetadata>? permissions)
            ? permissions
            : [];
}

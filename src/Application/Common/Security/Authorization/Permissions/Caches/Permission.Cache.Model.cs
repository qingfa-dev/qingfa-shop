namespace Application.Common.Security.Authorization.Permissions.Caches;

/// <summary>
/// Cache envelope that distinguishes a cache miss from a resolved empty permission set.
/// </summary>
/// <remarks>
/// Permission identifiers are case-insensitive, so the envelope copies incoming sets into a
/// case-insensitive set before they are cached or compared.
/// </remarks>
public sealed record PermissionCacheEntry
{
    /// <summary>Creates a cache entry from the supplied permission set.</summary>
    /// <param name="permissions">The cached permissions; <c>null</c> means the entry is a miss.</param>
    public PermissionCacheEntry(HashSet<string>? permissions)
    {
        Permissions = permissions is null
            ? null
            : new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets the cached permissions; <c>null</c> means the entry is a miss.</summary>
    public HashSet<string>? Permissions { get; }

    /// <summary>Gets a value indicating whether the entry carries a resolved permission set.</summary>
    public bool IsHit => Permissions is not null;

    /// <summary>Gets the shared miss marker.</summary>
    public static PermissionCacheEntry Miss { get; } = new((HashSet<string>?)null);

    /// <summary>Creates a hit entry for the supplied permission set.</summary>
    /// <param name="permissions">The resolved permissions.</param>
    /// <returns>The cache entry.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="permissions"/> is <c>null</c>.</exception>
    public static PermissionCacheEntry Hit(HashSet<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        return new PermissionCacheEntry(permissions);
    }
}

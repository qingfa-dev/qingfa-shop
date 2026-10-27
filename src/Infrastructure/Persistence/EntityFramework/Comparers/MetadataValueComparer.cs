using Core.Common.Monad.Metadata;

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Persistence.EntityFramework.Comparers;

/// <summary>
/// Value comparer for JSON-backed <see cref="DefaultMetadata"/> properties.
/// Without it, EF Core logs <c>CollectionWithoutComparer</c> and change tracking
/// can miss in-place dictionary edits.
/// </summary>
public static class MetadataValueComparer
{
    /// <summary>Gets the shared comparer instance.</summary>
    public static ValueComparer<DefaultMetadata> Instance { get; } = new(
        (left, right) => EqualsMetadata(left, right),
        value => HashMetadata(value),
        value => Snapshot(value));

    private static bool EqualsMetadata(DefaultMetadata? left, DefaultMetadata? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        return left.All(pair =>
            right.TryGetValue(pair.Key, out var value) && Equals(pair.Value, value));
    }

    private static int HashMetadata(DefaultMetadata value)
    {
        var hash = 0;
        foreach (var pair in value)
        {
            hash = HashCode.Combine(
                hash,
                pair.Key.GetHashCode(StringComparison.OrdinalIgnoreCase),
                pair.Value?.GetHashCode() ?? 0);
        }

        return hash;
    }

    private static DefaultMetadata Snapshot(DefaultMetadata value) => new(value);
}

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

/// <summary>Structural equality for composite grouping keys.</summary>
/// <remarks>
/// Values compare with <see cref="object.Equals(object?, object?)"/>, so strings group
/// case-sensitively, which can differ from a case-insensitive database collation.
/// </remarks>
internal sealed class ObjectArrayComparer : IEqualityComparer<object?[]>
{
    public static ObjectArrayComparer Instance { get; } = new();

    /// <summary>
    /// Compares two grouping key arrays element-by-element using object.Equals,
    /// which handles null values without throwing.
    /// </summary>
    public bool Equals(object?[]? x, object?[]? y)
    {
        // Short-circuit: same reference means equal.
        if (ReferenceEquals(x, y))
            return true;
        // Guard: null arrays and different lengths are not equal.
        if (x is null || y is null || x.Length != y.Length)
            return false;

        // Compare: element-by-element using object.Equals to handle null values without throwing.
        for (int index = 0; index < x.Length; index++)
        {
            if (!object.Equals(x[index], y[index]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Computes a combined hash code for all elements in the grouping key array.
    /// </summary>
    public int GetHashCode(object?[] obj)
    {
        // Combine: hash all elements so equal arrays produce the same hash.
        var hash = new HashCode();
        foreach (object? value in obj)
            hash.Add(value);
        return hash.ToHashCode();
    }
}
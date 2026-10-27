namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

/// <summary>Structural equality for composite grouping keys.</summary>
/// <remarks>
/// Values compare with <see cref="object.Equals(object?, object?)"/>, so strings group
/// case-sensitively, which can differ from a case-insensitive database collation.
/// </remarks>
internal sealed class ObjectArrayComparer : IEqualityComparer<object?[]>
{
    public static ObjectArrayComparer Instance { get; } = new();

    public bool Equals(object?[]? x, object?[]? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null || x.Length != y.Length)
            return false;

        for (int index = 0; index < x.Length; index++)
        {
            if (!object.Equals(x[index], y[index]))
                return false;
        }

        return true;
    }

    public int GetHashCode(object?[] obj)
    {
        var hash = new HashCode();
        foreach (object? value in obj)
            hash.Add(value);
        return hash.ToHashCode();
    }
}
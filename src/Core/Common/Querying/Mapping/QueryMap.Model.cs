using System.Diagnostics.CodeAnalysis;

namespace Core.Common.Querying.Mapping;

/// <summary>An explicit, immutable allowlist of fields a query may address.</summary>
/// <remarks>
/// A map is produced by <see cref="QueryMapBuilder{T}"/> and never changes
/// afterwards. Only a build that reported no failure produces one, so a map that
/// reaches the registry is always sound.
/// </remarks>
public sealed class QueryMap<T>
{
    private readonly IReadOnlyDictionary<string, QueryField> _fields;

    internal QueryMap(IReadOnlyDictionary<string, QueryField> fields) =>
        _fields = fields;

    /// <summary>Gets every mapped field.</summary>
    public IReadOnlyCollection<QueryField> Fields => _fields.Values.ToArray();

    /// <summary>Looks a field up by name.</summary>
    /// <param name="name">The name to look up.</param>
    /// <param name="field">The mapped field, when one matches.</param>
    /// <returns>True when the name is mapped.</returns>
    public bool TryGet(string name, [NotNullWhen(true)] out QueryField? field) =>
        _fields.TryGetValue(name, out field);

    /// <summary>Looks a field up by name.</summary>
    /// <param name="name">The name to look up.</param>
    /// <returns>The mapped field, or a failure when the name is not mapped.</returns>
    public Result<QueryField> Get(string name) =>
        _fields.TryGetValue(name, out QueryField? field)
            ? Result<QueryField>.Success(field)
            : Result<QueryField>.Fail(QueryOutcome.Failure.NotMapped(name));
}
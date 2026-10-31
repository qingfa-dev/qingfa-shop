using System.Diagnostics.CodeAnalysis;

namespace Application.BuildingBlocks.Querying.Mapping;

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
    /// <param name="name">The public field name used in query strings (e.g., "Status").</param>
    /// <param name="field">The mapped field metadata, or null when the name is not found.</param>
    /// <returns>True when the name is mapped.</returns>
    public bool TryGet(string name, [NotNullWhen(true)] out QueryField? field) =>
        _fields.TryGetValue(name, out field);

    /// <summary>Looks a field up by name, returning a typed result.</summary>
    /// <param name="name">The public field name used in query strings (e.g., "Status").</param>
    /// <returns>The mapped field metadata, or a failure with a NotMapped error when the name is not found.</returns>
    public Result<QueryField> Get(string name) =>
        _fields.TryGetValue(name, out QueryField? field)
            ? Result<QueryField>.Success(field)
            : Result<QueryField>.Fail(QueryOutcome.Failure.NotMapped(name));
}
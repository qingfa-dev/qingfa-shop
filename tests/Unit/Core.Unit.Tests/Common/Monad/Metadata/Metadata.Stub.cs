namespace Core.Unit.Tests.Common.Monad.Metadata;

/// <summary>
/// Stubs used across metadata extension-method specifications.
/// </summary>
#region Stubs

/// <summary>
/// Minimal mutable <see cref="IMetadata"/> implementation backed by
/// <see cref="DefaultMetadata"/>.
/// </summary>
public sealed class MetadataStub : IMetadata
{
    private readonly DefaultMetadata _metadata;

    public MetadataStub()
    {
        _metadata = DefaultMetadata.Create();
    }

    public object this[string key]
        => _metadata[key];

    public IEnumerable<string> Keys
        => _metadata.Keys;

    public IEnumerable<object> Values
        => _metadata.Values;

    public int Count
        => _metadata.Count;

    public DefaultMetadata Metadata
        => _metadata;

    public bool ContainsKey(string key)
        => _metadata.ContainsKey(key);

    public bool TryGetValue(
        string key,
        out object value)
        => _metadata.TryGetValue(key, out value!);

    public MetadataStub With(
        string key,
        object value)
    {
        _metadata[key] = value;
        return this;
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        => _metadata.GetEnumerator();

    System.Collections.IEnumerator
        System.Collections.IEnumerable.GetEnumerator()
        => GetEnumerator();

    public void SetRawValue(string key, object value)
        => _metadata[key] = value;
}

public sealed class HasMetadataStub<TMetadata> : IHasMetadata<TMetadata>
    where TMetadata : IMetadata
{
    public required TMetadata Metadata { get; init; }
}
/// <summary>
/// Opaque type with no meaningful conversion path.
/// </summary>
public sealed class StubOpaque
{
    public override string? ToString()
        => null;
}

#endregion
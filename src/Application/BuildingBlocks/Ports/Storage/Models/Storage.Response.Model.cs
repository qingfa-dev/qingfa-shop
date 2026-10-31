namespace Application.BuildingBlocks.Ports.Storage.Models;

/// <summary>Metadata returned after a successful upload.</summary>
public sealed record UploadResult
{
    /// <summary>The key under which the object was stored.</summary>
    public string Key { get; init; } = default!;

    /// <summary>Name of the provider that accepted the upload.</summary>
    public string Provider { get; init; } = default!;

    /// <summary>Public or pre-signed URI to the stored object, if applicable.</summary>
    public Uri? Uri { get; init; }

    /// <summary>Persisted byte size as reported by the provider.</summary>
    public long SizeBytes { get; init; }

    /// <summary>UTC timestamp of when the object was committed.</summary>
    public DateTimeOffset StoredAtUtc { get; init; }
}
 
/// <summary>Metadata for a stored object returned by a listing or stat operation.</summary>
public sealed record StoredObjectInfo
{
    /// <summary>Object key.</summary>
    public string Key { get; init; } = default!;

    /// <summary>Provider that holds the object.</summary>
    public string Provider { get; init; } = default!;

    /// <summary>Byte size of the object.</summary>
    public long SizeBytes { get; init; }

    /// <summary>UTC timestamp of the last write.</summary>
    public DateTimeOffset LastModifiedUtc { get; init; }

    /// <summary>MIME type, if known.</summary>
    public string? ContentType { get; init; }
}
 
/// <summary>Carries a downloaded object's byte stream and its metadata.</summary>
public sealed record DownloadResult
{
    /// <summary>Readable stream of the object's bytes.</summary>
    public Stream Content { get; init; } = default!;

    /// <summary>Metadata associated with the downloaded object.</summary>
    public StoredObjectInfo Info { get; init; } = default!;
}
 
namespace Application.BuildingBlocks.Ports.Storage.Models;

/// <summary>Encapsulates the data required to upload an object to a storage provider.</summary>
public sealed record UploadRequest
{
    /// <summary>Logical path / filename for the object (e.g. <c>avatars/user-42.png</c>).</summary>
    public string Key { get; init; } = default!;

    /// <summary>The raw bytes to persist.</summary>
    public Stream Content { get; init; } = default!;

    /// <summary>MIME type of the content (e.g. <c>image/png</c>).</summary>
    public string ContentType { get; init; } = default!;

    /// <summary>Optional key-value metadata to attach to the object.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; } = null;

    public UploadOptions? Options { get; init; } = null;
}


public sealed record UploadOptions
{
    public bool Overwrite { get; init; }
    public bool GenerateHash { get; init; } = true;
    public bool Encrypt { get; init; }

    // Malware
    public bool ScanForMalware { get; init; }
    public int MalwareScanTimeoutSeconds { get; init; } = 30;
    public InfectionAction OnMalwareDetected { get; init; }

    // Image resizing
    public int? ResizeWidth { get; init; }
    public int? ResizeHeight { get; init; }
    public ResizeMode ResizeMode { get; init; }
    public bool MaintainAspectRatio { get; init; } = true;
    public string? OutputFormat { get; init; }
}
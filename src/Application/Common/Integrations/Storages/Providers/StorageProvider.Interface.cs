using Application.Common.Integrations.Storages.Models;

namespace Application.Common.Integrations.Storages.Providers;

/// <summary>
/// Abstraction for a named storage provider capable of uploading, downloading,
/// deleting, and listing objects in a backing store (local disk, S3, Azure Blob, etc.).
/// </summary>
public interface IStorageProvider
{
    /// <summary>Gets the unique name identifying this provider (e.g. <c>"local"</c>, <c>"s3"</c>).</summary>
    string Name { get; }

    /// <summary>
    /// Uploads the object described by <paramref name="request"/> to the backing store.
    /// </summary>
    /// <param name="request">Upload descriptor including key, stream, and MIME type.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> carrying an <see cref="UploadResult"/> on success,
    /// or a failure result with an error code.
    /// </returns>
    Task<Result<UploadResult>> UploadAsync(UploadRequest request, CancellationToken ct = default);

    /// <summary>Downloads the object identified by <paramref name="key"/>.</summary>
    /// <param name="key">Object key as stored.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<DownloadResult>> DownloadAsync(string key, CancellationToken ct = default);

    /// <summary>Resolves the physical file path for <paramref name="key"/> if this is a file-based provider.</summary>
    /// <param name="key">Object key as stored.</param>
    /// <returns>The full physical file path, or an error for non-file-based providers.</returns>
    Result<string> ResolvePath(string key);

    /// <summary>Deletes the object identified by <paramref name="key"/>.</summary>
    /// <param name="key">Object key to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>Returns metadata for the object identified by <paramref name="key"/>, or a not-found result.</summary>
    /// <param name="key">Object key to stat.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<StoredObjectInfo>> StatAsync(string key, CancellationToken ct = default);

    /// <summary>Lists keys under the given <paramref name="prefix"/>.</summary>
    /// <param name="prefix">Key prefix filter; pass <c>null</c> to list all.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<IReadOnlyList<StoredObjectInfo>>> ListAsync(string? prefix, CancellationToken ct = default);
}

using Application.BuildingBlocks.Ports.Storage.Models;
using Application.BuildingBlocks.Ports.Storage.Providers;

namespace Application.BuildingBlocks.Ports.Storage.Services;

/// <summary>
/// High-level storage service that routes operations to the correct <see cref="IStorageProvider"/>
/// and applies cross-cutting concerns (security, anti-forgery, auditing).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads an object using the <paramref name="providerName"/> provider.
    /// Pass <c>null</c> to use the default provider.
    /// </summary>
    Task<Result<UploadResult>> UploadAsync(
        UploadRequest request,
        string? providerName = null,
        UploadOptions? options = null,
        CancellationToken ct = default);

    /// <summary>Downloads an object from the <paramref name="providerName"/> provider.</summary>
    Task<Result<DownloadResult>> DownloadAsync(
        string key,
        string? providerName = null,
        CancellationToken ct = default);

    /// <summary>Resolves the physical file path using the <paramref name="providerName"/> provider.</summary>
    /// <param name="key">Object key as stored.</param>
    /// <param name="providerName">Provider name; pass <c>null</c> for default.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The full physical file path, or an error for non-file-based providers.</returns>
    Task<Result<string>> ResolvePathAsync(
        string key,
        string? providerName = null,
        CancellationToken ct = default);

    /// <summary>Deletes an object from the <paramref name="providerName"/> provider.</summary>
    Task<Result> DeleteAsync(
        string key,
        string? providerName = null,
        CancellationToken ct = default);

    /// <summary>Stats an object from the <paramref name="providerName"/> provider.</summary>
    Task<Result<StoredObjectInfo>> StatAsync(
        string key,
        string? providerName = null,
        CancellationToken ct = default);

    /// <summary>Lists objects under <paramref name="prefix"/> from the <paramref name="providerName"/> provider.</summary>
    Task<Result<IReadOnlyList<StoredObjectInfo>>> ListAsync(
        string? prefix = null,
        string? providerName = null,
        CancellationToken ct = default);
}
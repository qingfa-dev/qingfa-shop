using Microsoft.Extensions.Options;

using Application.Common.Integrations.Storages.Models;

namespace Infrastructure.Integrations.Storages.Providers;

/// <summary>Local filesystem storage provider with path traversal protection, async I/O, and configurable buffer size.</summary>
// Invariant: All file paths are resolved relative to LocalPath root; path traversal is rejected via prefix check; null URI since local FS has no public endpoint.
// Context: Path traversal is the primary threat for local storage (Threat TMT-FILE-003). ResolvePath applies Path.GetFullPath normalization to prevent ../ escapes.
// Boundary: Provider → Local Filesystem — direct I/O operations; caller owns stream disposal for downloads.
public sealed partial class LocalStorageProvider(
    IOptions<LocalStorageProviderSetting> options,
    ILogger<LocalStorageProvider> logger)
    : IStorageProvider
{
    public string Name => "local";

    /// <summary>Writes a file to the local filesystem with optional directory creation.</summary>
    // Contract: pre=request!=null && request.Key!=null, post=return.IsSuccess implies file written, throws=OperationCanceledException on cancellation
    public async Task<Result<UploadResult>> UploadAsync(UploadRequest request, CancellationToken ct = default)
    {
        // Guard: honour caller cancellation before initiating any I/O
        ct.ThrowIfCancellationRequested();

        // Guard: resolve and validate key — rejects path traversal (TMT-FILE-003)
        var pathResult = ResolvePath(request.Key);
        if (!pathResult.IsSuccess)
            return pathResult.Errors;
        var fullPath = pathResult.Value;

        // Create: ensure target directory exists before opening stream
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        try
        {
            var bufferSize = options.Value.BufferSize;
            await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
            await request.Content.CopyToAsync(fileStream, bufferSize, ct);
            await fileStream.FlushAsync(ct);

            var fileInfo = new FileInfo(fullPath);
            var size = fileInfo.Length;
            Loggers.LogUploadSuccess(logger, request.Key, size);

            // Transform: return result with null URI — local FS has no public endpoint
            return Result<UploadResult>.Ok(new UploadResult
            {
                Key = request.Key,
                Provider = Name,
                Uri = null,
                SizeBytes = size,
                StoredAtUtc = DateTimeOffset.UtcNow
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Catch: file I/O error during write — surface as provider error
            Loggers.LogUploadFailed(logger, request.Key, ex);
            return Result<UploadResult>.Fail(StorageResult.Failure.ProviderError(ex.Message));
        }
    }

    /// <summary>Reads a file from the local filesystem — caller owns the returned stream.</summary>
    // Contract: pre=key!=null, post=return.IsSuccess implies stream opened, throws=OperationCanceledException on cancellation
    public Task<Result<DownloadResult>> DownloadAsync(string key, CancellationToken ct = default)
    {
        // Guard: honour caller cancellation before initiating any I/O
        ct.ThrowIfCancellationRequested();

        // Guard: resolve and validate key — rejects path traversal
        var pathResult = ResolvePath(key);
        if (!pathResult.IsSuccess)
            return Task.FromResult<Result<DownloadResult>>(pathResult.Errors);
        var fullPath = pathResult.Value;

        // Validate: file must exist before opening stream
        if (!File.Exists(fullPath))
            return Task.FromResult<Result<DownloadResult>>(StorageResult.Failure.NotFound(key));

        try
        {
            // Acquire: open shared-read stream — caller owns disposal
            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, options.Value.BufferSize, useAsync: true);
            var info = new FileInfo(fullPath);
            // Create: StoredObjectInfo with ContentType=null — local FS lacks MIME metadata
            var meta = new StoredObjectInfo
            {
                Key = key,
                Provider = Name,
                SizeBytes = info.Length,
                LastModifiedUtc = info.LastWriteTimeUtc,
                ContentType = null
            };
            return Task.FromResult(Result<DownloadResult>.Ok(new DownloadResult { Content = stream, Info = meta }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Catch: file I/O error during read — surface as provider error
            Loggers.LogDownloadFailed(logger, key, ex);
            return Task.FromResult(
                Result<DownloadResult>.Fail(StorageResult.Failure.ProviderError(ex.Message)));
        }
    }

    /// <summary>Deletes a file from the local filesystem.</summary>
    // Contract: pre=key!=null, post=file deleted if exists, throws=OperationCanceledException on cancellation
    public Task<Result> DeleteAsync(string key, CancellationToken ct = default)
    {
        // Guard: honour caller cancellation before initiating any I/O
        ct.ThrowIfCancellationRequested();

        // Guard: resolve and validate key — rejects path traversal
        var pathResult = ResolvePath(key);
        if (!pathResult.IsSuccess)
            return Task.FromResult<Result>(pathResult.Errors);
        var fullPath = pathResult.Value;

        // Validate: file must exist before attempting delete
        if (!File.Exists(fullPath))
            return Task.FromResult<Result>(StorageResult.Failure.NotFound(key));

        try
        {
            File.Delete(fullPath);
            Loggers.LogDeleteSuccess(logger, key);
            return Task.FromResult(Result.Ok());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Catch: file I/O error during delete — surface as provider error
            Loggers.LogDeleteFailed(logger, key, ex);
            return Task.FromResult(
                Result.Fail(StorageResult.Failure.ProviderError(ex.Message)));
        }
    }

    /// <summary>Gets file metadata from the local filesystem.</summary>
    // Contract: pre=key!=null, post=return.IsSuccess implies metadata read, throws=OperationCanceledException on cancellation
    public Task<Result<StoredObjectInfo>> StatAsync(string key, CancellationToken ct = default)
    {
        // Guard: honour caller cancellation before initiating any I/O
        ct.ThrowIfCancellationRequested();

        // Guard: resolve and validate key — rejects path traversal
        var pathResult = ResolvePath(key);
        if (!pathResult.IsSuccess)
            return Task.FromResult<Result<StoredObjectInfo>>(pathResult.Errors);
        var fullPath = pathResult.Value;

        // Validate: file must exist before querying metadata
        if (!File.Exists(fullPath))
            return Task.FromResult<Result<StoredObjectInfo>>(StorageResult.Failure.NotFound(key));

        try
        {
            var info = new FileInfo(fullPath);
            return Task.FromResult(Result<StoredObjectInfo>.Ok(
                new StoredObjectInfo { Key = key, Provider = Name, SizeBytes = info.Length, LastModifiedUtc = info.LastWriteTimeUtc, ContentType = null }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Catch: file I/O error during stat — surface as provider error
            Loggers.LogStatFailed(logger, key, ex);
            return Task.FromResult(
                Result<StoredObjectInfo>.Fail(StorageResult.Failure.ProviderError(ex.Message)));
        }
    }

    /// <summary>Lists all files under the storage root, optionally filtered by prefix.</summary>
    // Contract: pre=none, post=return.IsSuccess, throws=OperationCanceledException on cancellation
    public Task<Result<IReadOnlyList<StoredObjectInfo>>> ListAsync(string? prefix, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var root = options.Value.LocalPath;

        // Guard: storage root does not exist — return empty list, not an error
        if (!Directory.Exists(root))
            return Task.FromResult(Result<IReadOnlyList<StoredObjectInfo>>.Ok([]));

        try
        {
            // Batch: enumerate all files recursively — filter by prefix after projection
            var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(f =>
                {
                    var relativeKey = Path.GetRelativePath(root, f).Replace('\\', '/');
                    var fi = new FileInfo(f);
                    return new StoredObjectInfo { Key = relativeKey, Provider = Name, SizeBytes = fi.Length, LastModifiedUtc = fi.LastWriteTimeUtc, ContentType = null };
                })
                .Where(o => prefix is null || o.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult(Result<IReadOnlyList<StoredObjectInfo>>.Ok(files.AsReadOnly()));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Catch: file I/O error during directory enumeration — surface as provider error
            Loggers.LogListFailed(logger, ex);
            return Task.FromResult(
                Result<IReadOnlyList<StoredObjectInfo>>.Fail(StorageResult.Failure.ProviderError(ex.Message)));
        }
    }

    /// <summary>Resolves a storage key to a safe local path with path traversal protection.</summary>
    // Contract: pre=key!=null, post=return.IsSuccess implies combined path within root, throws=never
    public Result<string> ResolvePath(string key)
    {
        try
        {
            // Guard: normalize root path and combine with request key
            var root = Path.GetFullPath(options.Value.LocalPath);
            var combined = Path.GetFullPath(Path.Combine(root, key.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

            // Guard: prevent path traversal — combined path must stay within root (TMT-FILE-003)
            if (!combined.StartsWith(root, StringComparison.Ordinal))
                return StorageResult.Failure.PathTraversalDetected(key);

            return Result<string>.Ok(combined);
        }
        catch (Exception ex)
        {
            // Catch: path resolution failed (invalid chars, etc.) — surface as provider error
            return Result<string>.Fail(StorageResult.Failure.ProviderError(ex.Message));
        }
    }
}

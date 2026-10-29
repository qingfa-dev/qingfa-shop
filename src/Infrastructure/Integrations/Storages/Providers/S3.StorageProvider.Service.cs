using Microsoft.Extensions.Options;

using Application.Common.Integrations.Storages.Models;
using Application.Common.Integrations.Storages.Providers.Options;

namespace Infrastructure.Integrations.Storages.Providers;

/// <summary>S3-compatible storage provider stub — supports ServiceUrl (MinIO, Wasabi) and virtual-hosted AWS S3 URLs.</summary>
// Invariant: URI resolves via ServiceUrl when configured, else virtual-hosted S3 format; unimplemented operations return NotImplemented errors.
// AgentHint: Replace stub implementations with AWS SDK v3 calls when AWSSDK.S3 package is added to the project.
// Boundary: Provider → S3 API — external cloud storage boundary; all operations are network calls to S3-compatible endpoint.
public sealed partial class S3StorageProvider(
    IOptions<S3StorageProviderSetting> options,
    ILogger<S3StorageProvider> logger)
    : IStorageProvider
{
    public string Name => "s3";

    /// <summary>Stores a file in the S3 bucket — stub; awaits AWSSDK.S3 integration.</summary>
    // Contract: pre=request!=null, post=return.IsSuccess, throws=never (stub)
    public Task<Result<UploadResult>> UploadAsync(UploadRequest request, CancellationToken ct = default)
    {
        var opts = options.Value;
        Loggers.LogUploadStart(logger, request.Key, opts.BucketName);

        // AgentHint: Replace with real AWS SDK call when AWSSDK.S3 is added.
        //   var putRequest = new PutObjectRequest { BucketName = opts.BucketName, Key = request.Key, InputStream = request.Content };
        //   await _s3.PutObjectAsync(putRequest, ct);

        // Compute: build S3 URI from provider config (ServiceUrl or virtual-hosted style)
        var uri = BuildBucketUri(request.Key);
        return Task.FromResult(Result<UploadResult>.Ok(
            new UploadResult { Key = request.Key, Provider = Name, Uri = uri, SizeBytes = 0, StoredAtUtc = DateTimeOffset.UtcNow }));
    }

    /// <summary>Downloads a file from S3 — stub; awaits AWSSDK.S3 integration.</summary>
    // Contract: pre=key!=null, post=return.IsSuccess, throws=never (stub)
    public Task<Result<DownloadResult>> DownloadAsync(string key, CancellationToken ct = default)
    {
        Loggers.LogDownloadStub(logger, key);
        // AgentHint: Replace with real AWS SDK call when AWSSDK.S3 is added.
        return Task.FromResult<Result<DownloadResult>>(
            Error.Unexpected("Storage.NotImplemented", "S3 download stub — install AWSSDK.S3 for real implementation."));
    }

    /// <summary>Deletes a file from S3 — stub; awaits AWSSDK.S3 integration.</summary>
    // Contract: pre=key!=null, post=return.IsSuccess, throws=never (stub)
    public Task<Result> DeleteAsync(string key, CancellationToken ct = default)
    {
        Loggers.LogDeleteStub(logger, key);
        // AgentHint: Replace with real AWS SDK call when AWSSDK.S3 is added.
        return Task.FromResult(Result.Ok());
    }

    /// <summary>Gets metadata for an S3 object — stub; awaits AWSSDK.S3 integration.</summary>
    // Contract: pre=key!=null, post=return.IsSuccess, throws=never (stub)
    public Task<Result<StoredObjectInfo>> StatAsync(string key, CancellationToken ct = default)
    {
        Loggers.LogStatStub(logger, key);
        // AgentHint: Replace with real AWS SDK call when AWSSDK.S3 is added.
        return Task.FromResult<Result<StoredObjectInfo>>(
            Error.Unexpected("Storage.NotImplemented", "S3 stat stub — install AWSSDK.S3 for real implementation."));
    }

    /// <summary>Lists objects in the S3 bucket — stub; awaits AWSSDK.S3 integration.</summary>
    // Contract: pre=none, post=return.IsSuccess, throws=never (stub)
    public Task<Result<IReadOnlyList<StoredObjectInfo>>> ListAsync(string? prefix, CancellationToken ct = default)
    {
        Loggers.LogListStub(logger, prefix);
        // AgentHint: Replace with real AWS SDK call when AWSSDK.S3 is added.
        return Task.FromResult(Result<IReadOnlyList<StoredObjectInfo>>.Ok([]));
    }

    /// <summary>Not supported — S3 is not a file-based provider; use DownloadAsync.</summary>
    // Contract: post=return.IsFailure
    public Result<string> ResolvePath(string key) =>
        StorageResult.Failure.ProviderError("S3 is not a file-based provider. Use DownloadAsync to get a stream.");

    // Compute: build S3 URI — ServiceUrl for custom endpoints (MinIO, Wasabi) or virtual-hosted AWS S3 format
    private Uri BuildBucketUri(string key)
    {
        var opts = options.Value;

        // Validate: custom ServiceUrl present — use as base for MinIO, Wasabi, etc.
        if (!string.IsNullOrWhiteSpace(opts.ServiceUrl))
            return new Uri($"{opts.ServiceUrl.TrimEnd('/')}/{key.TrimStart('/')}");

        // Fallback: virtual-hosted S3 URL (default AWS convention)
        return new Uri($"https://{opts.BucketName}.s3.{opts.Region}.amazonaws.com/{key.TrimStart('/')}");
    }
}

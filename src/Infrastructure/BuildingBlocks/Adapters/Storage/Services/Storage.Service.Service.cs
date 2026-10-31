using System.Diagnostics;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Infrastructure.BuildingBlocks.Adapters.Storage.Helpers;
using Application.BuildingBlocks.Ports.Storage.Models;
using Application.BuildingBlocks.Ports.Storage.Processing;
using Application.BuildingBlocks.Ports.Storage.Providers;
using Application.BuildingBlocks.Ports.Storage.Security;
using Application.BuildingBlocks.Ports.Storage.Security.Guard;
using Application.BuildingBlocks.Ports.Storage.Security.Options;
using Application.BuildingBlocks.Ports.Storage.Security.Scanners;

namespace Infrastructure.BuildingBlocks.Adapters.Storage.Services;

/// <summary>Orchestrates storage operations — routes by provider name, applies security enforcement, anti-forgery, and an optional pre-upload pipeline (hash, malware scan, image processing, encryption).</summary>
// Invariant: Provider lookup by name — falls back to defaultProviderName if unspecified; all operations timed via RunTimed wrapper.
// Boundary: Service → StorageProvider | SecurityEnforcer | MalwareScanner | ImageProcessor — orchestrates across multiple subsystems; never accesses I/O or cache directly.
// Context: Upload pipeline applies sequential transforms (hash → malware scan → image processing → encryption) to mitigate TMT-FILE-001 through TMT-FILE-004.
public sealed partial class StorageService(
    IReadOnlyDictionary<string, IStorageProvider> providers,
    string defaultProviderName,
    IStorageSecurityEnforcer enforcer,
    IStorageAntiForgeryGuard antiforgeryGuard,
    IHttpContextAccessor httpContextAccessor,
    ILogger<StorageService> logger,
    IStorageMalwareScanner? malwareScanner = null,
    IImageProcessor? imageProcessor = null,
    IOptions<StorageSecuritySetting>? storageSecurityOptions = null)
    : IStorageService
{
    /// <summary>Uploads content through the security and processing pipeline, then delegates to the resolved storage provider.</summary>
    // Contract: pre=request!=null && request.Key!=null && request.Content!=null, post=return.IsSuccess implies uploaded, throws=never
    public async Task<Result<UploadResult>> UploadAsync(
        UploadRequest request,
        string? providerName = null,
        UploadOptions? options = null,
        CancellationToken ct = default)
    {
        if (!TryResolve(providerName, out IStorageProvider provider))
            return StorageOutcome.Failure.ProviderNotFound(providerName ?? defaultProviderName);

        // Validate: anti-forgery CSRF token with rate-limited failure tracking (TMT-CSRF-001)
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            string identityKey = ExtractIdentityKey(httpContext);
            Result guardResult = await antiforgeryGuard.ValidateRequestAsync(identityKey, httpContext, ct);
            if (!guardResult.IsSuccess)
            {
                Loggers.LogUploadBlocked(logger, request.Key, FirstErrorCode(guardResult.Errors) ?? "antiforgery failure");
                return guardResult.Errors;
            }
        }

        // Validate: run security rules — extension allowlist, size cap, magic bytes (module boundary: Service → SecurityEnforcer)
        Result securityResult = await enforcer.EnforceAsync(request, ct);
        if (!securityResult.IsSuccess)
        {
            Loggers.LogSecurityCheckFailed(logger, request.Key, FirstErrorCode(securityResult.Errors));
            return securityResult.Errors;
        }

        // Merge: method-level options override request-level defaults
        UploadOptions effectiveOptions = options ?? request.Options ?? new();

        // Track: accumulated metadata from each pipeline stage
        Dictionary<string, string>? pipelineMetadata = null;

        // Pipeline: hash content before any transforms — verifiable integrity later
        Stream content = request.Content;
        if (effectiveOptions.GenerateHash)
        {
            if (content.CanSeek)
                content.Position = 0;

            try
            {
                string hash = await HashHelper.ComputeHashAsync(content, ct);
                pipelineMetadata ??= [];
                pipelineMetadata["content-hash"] = hash;
                Loggers.LogHashComputed(logger, request.Key, hash);
            }
            catch (Exception ex)
            {
                // Catch: hash failure blocks upload — content integrity cannot be verified
                Loggers.LogHashFailed(logger, request.Key, ex.Message);
                return Result<UploadResult>.Fail(StorageOutcome.Failure.HashFailed(ex.Message));
            }
        }

        // Pipeline: scan for malware via ClamAV or content scanner (module boundary: Service → MalwareScanner)
        if (effectiveOptions.ScanForMalware && malwareScanner is not null)
        {
            if (content.CanSeek)
                content.Position = 0;

            Result<MalwareScanResult> scanResult = await malwareScanner.ScanAsync(content, request.Key, effectiveOptions, ct);
            if (!scanResult.IsSuccess)
            {
                Loggers.LogMalwareScanFailed(logger, request.Key, FirstErrorCode(scanResult.Errors));
                return scanResult.Errors;
            }

            if (!scanResult.Value.IsClean)
            {
                string threat = scanResult.Value.ThreatName ?? "unknown";

                // Validate: rejection policy — block upload when malware detected
                if (effectiveOptions.OnMalwareDetected == InfectionAction.Reject)
                {
                    Loggers.LogMalwareRejected(logger, request.Key, threat);
                    return StorageOutcome.Failure.MalwareRejected(threat);
                }

                // Validate: quarantine policy — mark metadata, allow upload
                if (effectiveOptions.OnMalwareDetected == InfectionAction.Quarantine)
                {
                    Loggers.LogMalwareQuarantined(logger, request.Key, threat);
                    pipelineMetadata ??= [];
                    pipelineMetadata["quarantine-threat"] = threat;
                    pipelineMetadata["quarantine-timestamp"] = DateTimeOffset.UtcNow.ToString("O");
                }

                // Validate: warning policy — log threat, allow upload with metadata flag
                if (effectiveOptions.OnMalwareDetected == InfectionAction.AllowWithWarning)
                {
                    Loggers.LogMalwareWarning(logger, request.Key, threat);
                    pipelineMetadata ??= [];
                    pipelineMetadata["malware-threat"] = threat;
                }
            }
        }

        // Pipeline: process image (resize, format conversion) via SkiaSharp (module boundary: Service → ImageProcessor)
        if (imageProcessor is not null && (effectiveOptions.ResizeWidth is not null || effectiveOptions.ResizeHeight is not null || effectiveOptions.OutputFormat is not null))
        {
            if (content.CanSeek)
                content.Position = 0;

            Result<Stream> processResult = await imageProcessor.ProcessAsync(content, effectiveOptions, ct);
            if (!processResult.IsSuccess)
            {
                Loggers.LogImageProcessingFailed(logger, request.Key, FirstErrorCode(processResult.Errors));
                return processResult.Errors;
            }

            content = processResult.Value;
            Loggers.LogImageProcessingCompleted(logger, request.Key);
        }

        // Pipeline: encrypt content at rest using configured key (TMT-FILE-004)
        if (effectiveOptions.Encrypt)
        {
            string? encryptionKey = storageSecurityOptions?.Value?.EncryptionKey;
            if (string.IsNullOrEmpty(encryptionKey))
            {
                // Guard: no encryption key configured — skip with warning, not error
                Loggers.LogEncryptionSkipped(logger, request.Key);
            }
            else
            {
                if (content.CanSeek)
                    content.Position = 0;

                try
                {
                    byte[] key = Encoding.UTF8.GetBytes(encryptionKey);
                    content = await EncryptionHelper.EncryptAsync(content, key, ct);
                    pipelineMetadata ??= [];
                    pipelineMetadata["encrypted"] = "true";
                    Loggers.LogEncryptionApplied(logger, request.Key);
                }
                catch (Exception ex)
                {
                    // Catch: encryption failure blocks upload — content would be stored in plaintext
                    Loggers.LogEncryptionFailed(logger, request.Key, ex.Message);
                    return Result<UploadResult>.Fail(StorageOutcome.Failure.EncryptionFailed(ex.Message));
                }
            }
        }

        // Transform: mark overwrite in metadata for provider-level handling
        if (effectiveOptions.Overwrite)
        {
            pipelineMetadata ??= [];
            pipelineMetadata["overwrite-existing"] = "true";
        }

        // Merge: combine pipeline metadata with request metadata for final upload
        IReadOnlyDictionary<string, string>? mergedMetadata = request.Metadata;
        if (pipelineMetadata is not null)
        {
            var combined = new Dictionary<string, string>(request.Metadata ?? new Dictionary<string, string>());
            foreach (var kvp in pipelineMetadata)
                combined[kvp.Key] = kvp.Value;
            mergedMetadata = combined;
        }

        // Rebuild: request with processed content and merged metadata
        UploadRequest uploadRequest = request with { Content = content, Metadata = mergedMetadata };

        // Call: delegate to the resolved provider with timing (module boundary: Service → Provider)
        Stopwatch sw = Stopwatch.StartNew();
        Result<UploadResult> result = await provider.UploadAsync(uploadRequest, ct);
        sw.Stop();

        if (result.IsSuccess)
        {
            Loggers.LogUploadSuccess(logger, provider.Name, request.Key, sw.ElapsedMilliseconds);
        }
        else
        {
            Loggers.LogUploadFailure(logger, provider.Name, request.Key, sw.ElapsedMilliseconds, FirstErrorCode(result.Errors));
        }

        return result;
    }

    /// <summary>Downloads a stored object by key from the resolved provider.</summary>
    // Contract: pre=key!=null, post=return.IsSuccess implies stream and metadata returned, throws=never
    public Task<Result<DownloadResult>> DownloadAsync(
        string key,
        string? providerName = null,
        CancellationToken ct = default)
        => TryResolve(providerName, out IStorageProvider provider)
            ? RunTimed(provider.Name, nameof(DownloadAsync), key, () => provider.DownloadAsync(key, ct))
            : Task.FromResult<Result<DownloadResult>>(StorageOutcome.Failure.ProviderNotFound(providerName ?? defaultProviderName));

    /// <summary>Resolves the storage path for a key without downloading content.</summary>
    // Contract: pre=key!=null, post=return.IsSuccess implies path resolved, throws=never
    public Task<Result<string>> ResolvePathAsync(
        string key,
        string? providerName = null,
        CancellationToken ct = default)
    {
        if (!TryResolve(providerName, out IStorageProvider provider))
            return Task.FromResult<Result<string>>(StorageOutcome.Failure.ProviderNotFound(providerName ?? defaultProviderName));

        Result<string> result = provider.ResolvePath(key);
        return Task.FromResult(result);
    }

    /// <summary>Deletes a stored object by key from the resolved provider.</summary>
    // Contract: pre=key!=null, post=return.IsSuccess implies deleted (if existed), throws=never
    public Task<Result> DeleteAsync(
        string key,
        string? providerName = null,
        CancellationToken ct = default)
        => TryResolve(providerName, out IStorageProvider provider)
            ? RunTimed(provider.Name, nameof(DeleteAsync), key, () => provider.DeleteAsync(key, ct))
            : Task.FromResult<Result>(StorageOutcome.Failure.ProviderNotFound(providerName ?? defaultProviderName));

    /// <summary>Gets metadata for a stored object by key from the resolved provider.</summary>
    // Contract: pre=key!=null, post=return.IsSuccess implies metadata returned, throws=never
    public Task<Result<StoredObjectInfo>> StatAsync(
        string key,
        string? providerName = null,
        CancellationToken ct = default)
        => TryResolve(providerName, out IStorageProvider provider)
            ? RunTimed(provider.Name, nameof(StatAsync), key, () => provider.StatAsync(key, ct))
            : Task.FromResult<Result<StoredObjectInfo>>(StorageOutcome.Failure.ProviderNotFound(providerName ?? defaultProviderName));

    /// <summary>Lists stored objects, optionally filtered by prefix, from the resolved provider.</summary>
    // Contract: pre=none, post=return.IsSuccess, throws=never
    public Task<Result<IReadOnlyList<StoredObjectInfo>>> ListAsync(
        string? prefix = null,
        string? providerName = null,
        CancellationToken ct = default)
        => TryResolve(providerName, out IStorageProvider provider)
            ? RunTimed(provider.Name, nameof(ListAsync), prefix ?? "(all)", () => provider.ListAsync(prefix, ct))
            : Task.FromResult<Result<IReadOnlyList<StoredObjectInfo>>>(StorageOutcome.Failure.ProviderNotFound(providerName ?? defaultProviderName));

    // ── Helpers ─────────────────────────────────────────────────────────────

    // Guard: resolve provider by name — falls back to default when name is null/empty
    private bool TryResolve(string? name, out IStorageProvider provider)
    {
        string key = string.IsNullOrWhiteSpace(name) ? defaultProviderName : name;
        if (providers.TryGetValue(key, out provider!))
            return true;
        Loggers.LogProviderNotFound(logger, key);
        return false;
    }

    // Compute: extract first error code from error list for structured logging
    private static string? FirstErrorCode(List<Error> errors)
        => errors.Count > 0 ? errors[0].Code : null;

    // Compute: extract user identity key from HTTP context for anti-forgery tracking
    private static string ExtractIdentityKey(HttpContext httpContext)
        => httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? httpContext.Connection.RemoteIpAddress?.ToString()
           ?? "anonymous";

    // Profile: execute action with timing — logs success/failure with duration
    private async Task<Result<T>> RunTimed<T>(
        string providerName, string operation, string key, Func<Task<Result<T>>> action)
    {
        Stopwatch sw = Stopwatch.StartNew();
        Result<T> result = await action();
        sw.Stop();
        if (result.IsSuccess)
        {
            Loggers.LogOperationSuccess(logger, providerName, operation, key, sw.ElapsedMilliseconds);
        }
        else
        {
            Loggers.LogOperationFailure(logger, providerName, operation, key, sw.ElapsedMilliseconds, FirstErrorCode(result.Errors));
        }
        return result;
    }

    // Profile: execute action with timing — non-generic overload for Result (not Result<T>)
    private async Task<Result> RunTimed(
        string providerName, string operation, string key, Func<Task<Result>> action)
    {
        Stopwatch sw = Stopwatch.StartNew();
        Result result = await action();
        sw.Stop();
        if (result.IsSuccess)
        {
            Loggers.LogOperationSuccess(logger, providerName, operation, key, sw.ElapsedMilliseconds);
        }
        else
        {
            Loggers.LogOperationFailure(logger, providerName, operation, key, sw.ElapsedMilliseconds, FirstErrorCode(result.Errors));
        }
        return result;
    }
}

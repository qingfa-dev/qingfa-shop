using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Application.Common.Integrations.Storages.Security.Guard.Options;
using Application.Common.Caching;

namespace Infrastructure.Integrations.Storages.Security.Guard;

/// <summary>Validates anti-forgery tokens for storage uploads with rate-limited failure tracking — mitigates TMT-CSRF-001.</summary>
// Invariant: Block threshold is MaxConsecutiveFailures from config; block duration resets on successful validation.
// Context: CSRF attack on upload endpoints (Threat TMT-CSRF-001). Failure counter cached with the block TTL for distributed rate limiting.
// Boundary: Guard → CacheService | ASP.NET Antiforgery — delegates CSRF validation to ASP.NET middleware; uses cache for failure tracking.
public sealed partial class StorageAntiForgeryGuard(
    ICacheService cacheService,
    IAntiforgery antiforgery,
    IOptions<AntiForgeryOptions> options,
    ILogger<StorageAntiForgeryGuard> logger)
    : IStorageAntiForgeryGuard
{
    private static string GetCacheKey(string identityKey)
        => $"antiforgery:failures:{identityKey}";

    private static CachingEntryOption CreateEntryOptions(TimeSpan blockDuration)
        => new()
        {
            Expiration = blockDuration,
            LocalCacheExpiration = blockDuration,
        };

    /// <summary>Validates an anti-forgery token — blocks after consecutive failures exceed threshold.</summary>
    // Contract: pre=identityKey!=null && httpContext!=null, post=return.IsSuccess if token valid, throws=never
    public async Task<Result> ValidateRequestAsync(string identityKey, HttpContext httpContext, CancellationToken ct = default)
    {
        // Guard: reject immediately if identity is already in blocked state
        if (await IsBlockedAsync(identityKey, ct))
            return StorageAntiForgeryGuardResult.Failure.TooManyAttempts();

        // Validate: check ASP.NET anti-forgery token
        if (!await antiforgery.IsRequestValidAsync(httpContext))
        {
            Loggers.LogAntiforgeryTokenInvalid(logger, identityKey);
            return await RecordFailureAsync(identityKey, ct);
        }

        // Reset: clear failure counter on successful validation
        return await ResetAsync(identityKey, ct);
    }

    /// <summary>Records a CSRF validation failure and blocks the identity after threshold exceeded.</summary>
    // Contract: pre=identityKey!=null, post=return.IsSuccess implies failure recorded, throws=never
    public async Task<Result> RecordFailureAsync(string identityKey, CancellationToken ct = default)
    {
        AntiForgeryOptions opts = options.Value;
        string cacheKey = GetCacheKey(identityKey);

        // Cache: read current failure count or start at 0
        Result<int> readResult = await cacheService.GetOrCreateAsync(
            cacheKey,
            static _ => ValueTask.FromResult(0),
            CreateEntryOptions(opts.BlockDuration),
            cancellationToken: ct);

        if (readResult.IsFailure)
            return Result.Fail(StorageAntiForgeryGuardResult.Failure.CacheUnavailable());

        int failures = readResult.Value + 1;

        // Cache: increment and persist with sliding block TTL
        Result writeResult = await cacheService.SetAsync(
            cacheKey,
            failures,
            CreateEntryOptions(opts.BlockDuration),
            cancellationToken: ct);

        if (writeResult.IsFailure)
            return Result.Fail(StorageAntiForgeryGuardResult.Failure.CacheUnavailable());

        // Validate: check if threshold reached — block identity
        if (failures >= opts.MaxConsecutiveFailures)
        {
            Loggers.LogIdentityBlocked(logger, identityKey, failures);
            return StorageAntiForgeryGuardResult.Failure.TooManyAttempts();
        }

        Loggers.LogFailureRecorded(logger, identityKey, failures);
        return Result.Ok();
    }

    /// <summary>Clears the failure counter for an identity on successful CSRF validation.</summary>
    // Contract: pre=identityKey!=null, post=failure counter cleared, throws=never
    public async Task<Result> ResetAsync(string identityKey, CancellationToken ct = default)
    {
        Result result = await cacheService.RemoveAsync(GetCacheKey(identityKey), ct);
        if (result.IsFailure)
            return Result.Fail(StorageAntiForgeryGuardResult.Failure.CacheUnavailable());

        Loggers.LogIdentityReset(logger, identityKey);
        return Result.Ok();
    }

    /// <summary>Checks whether an identity has exceeded the maximum consecutive failure threshold.</summary>
    // Contract: pre=identityKey!=null, post=return==true if blocked, throws=never
    public async Task<bool> IsBlockedAsync(string identityKey, CancellationToken ct = default)
    {
        Result<int> result = await cacheService.GetOrCreateAsync(
            GetCacheKey(identityKey),
            static _ => ValueTask.FromResult(0),
            cancellationToken: ct);

        // Guard: fail closed when the counter cannot be read.
        return result.IsFailure || result.Value >= options.Value.MaxConsecutiveFailures;
    }
}

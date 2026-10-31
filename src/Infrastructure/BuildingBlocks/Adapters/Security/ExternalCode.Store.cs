using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Application.BuildingBlocks.Security.Identity.Auth.Models;
using Application.BuildingBlocks.Security.Identity.Auth.Outcome;
using Application.BuildingBlocks.Security.Identity.External.Abstractions;
using Application.BuildingBlocks.Security.Identity.External.Options;

using Infrastructure.BuildingBlocks.Adapters.Serialization;

using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.BuildingBlocks.Adapters.Security;

/// <summary>Stores single-use codes that carry an issued external sign-in token pair.</summary>
/// <remarks>
/// The code is random, base64url-encoded, and returned to the caller once. Only its SHA-256
/// hash is used as the cache key, so a cache dump cannot be exchanged for tokens. The code
/// itself is never logged.
/// </remarks>
public sealed class ExternalCodeStore(IDistributedCache cache, ExternalAuthOption option)
    : IExternalCodeStore
{
    private const string KeyPrefix = "auth:external:";
    private const int CodeByteLength = 32;

    private TimeSpan Lifetime => TimeSpan.FromSeconds(Math.Max(1, option.CodeLifetimeSeconds));

    /// <inheritdoc />
    public async Task<Result<string>> StoreAsync(
        AuthTokenResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        string code = Base64UrlEncode(RandomNumberGenerator.GetBytes(CodeByteLength));
        string payload = JsonSerializer.Serialize(response, ApiJson.Options);

        try
        {
            await cache.SetStringAsync(
                KeyPrefix + Hash(code),
                payload,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = Lifetime
                },
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<string>.Fail(IdentityOutcome.Failure.ExternalCodeStoreFailed);
        }

        return Result<string>.Ok(code);
    }

    /// <inheritdoc />
    public async Task<Result<AuthTokenResponse>> ConsumeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        string key = KeyPrefix + Hash(code);
        string? payload = await cache.GetStringAsync(key, cancellationToken);

        if (payload is null)
            return InvalidCode();

        // Consume: remove before parsing so a replay cannot observe the same payload twice.
        await cache.RemoveAsync(key, cancellationToken);

        try
        {
            AuthTokenResponse? response = JsonSerializer.Deserialize<AuthTokenResponse>(
                payload,
                ApiJson.Options);

            return response is null
                ? Result<AuthTokenResponse>.Fail(IdentityOutcome.Failure.ExternalCodeCorrupt)
                : Result<AuthTokenResponse>.Ok(response);
        }
        catch (JsonException)
        {
            return Result<AuthTokenResponse>.Fail(IdentityOutcome.Failure.ExternalCodeCorrupt);
        }
    }

    private static Result<AuthTokenResponse> InvalidCode() =>
        Result<AuthTokenResponse>.Fail(IdentityOutcome.Failure.ExternalCodeInvalid);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

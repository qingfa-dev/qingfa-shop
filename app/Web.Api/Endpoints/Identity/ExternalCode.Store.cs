using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Application.Common.Security.Authentication.Identity;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Infrastructure.Integrations.Serialization;

using Microsoft.Extensions.Caching.Distributed;


using Application.Features.Identity.Auth.Models;

namespace Web.Api.Endpoints.Identity;

/// <summary>Stores single-use codes that carry an issued external sign-in token pair.</summary>
/// <remarks>
/// The code is random, base64url-encoded, and returned to the caller once. Only its SHA-256
/// hash is used as the cache key, so a cache dump cannot be exchanged for tokens. The code
/// itself is never logged.
/// </remarks>
public sealed class ExternalCodeStore(IDistributedCache cache, ExternalAuthOption option)
{
    private const string KeyPrefix = "auth:external:";
    private const int CodeByteLength = 32;

    private TimeSpan Lifetime => TimeSpan.FromSeconds(Math.Max(1, option.CodeLifetimeSeconds));

    /// <summary>Stores the token pair under a fresh one-time code.</summary>
    /// <param name="response">The token pair to hand to the client.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The plaintext code, or a failure when the cache write fails.</returns>
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
            return Result<string>.Fail(Error.Unexpected(
                "Auth.External.CodeStoreFailed",
                "The external sign-in code could not be stored."));
        }

        return Result<string>.Ok(code);
    }

    /// <summary>Reads and removes the token pair bound to a one-time code.</summary>
    /// <param name="code">The plaintext code from the client redirect.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The token pair, or a 401 failure when the code is unknown or expired.</returns>
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
                ? Result<AuthTokenResponse>.Fail(Error.Unexpected(
                    "Auth.External.CodeInvalid",
                    "The external sign-in code is invalid."))
                : Result<AuthTokenResponse>.Ok(response);
        }
        catch (JsonException)
        {
            return Result<AuthTokenResponse>.Fail(Error.Unexpected(
                "Auth.External.CodeInvalid",
                "The external sign-in code is invalid."));
        }
    }

    private static Result<AuthTokenResponse> InvalidCode() =>
        Result<AuthTokenResponse>.Fail(Error.Unauthorized(
            "Auth.External.CodeInvalid",
            "The external sign-in code is invalid or expired."));

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

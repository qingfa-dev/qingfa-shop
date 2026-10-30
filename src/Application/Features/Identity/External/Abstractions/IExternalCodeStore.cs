using Application.Features.Identity.Auth.Models;

namespace Application.Features.Identity.External.Abstractions;

/// <summary>Single-use handoff store for external sign-in token responses.</summary>
/// <remarks>
/// The external callback hands the client an opaque code; the exchange endpoint
/// consumes that code exactly once to obtain the token pair. The Redis-backed
/// implementation lives in Infrastructure.
/// </remarks>
public interface IExternalCodeStore
{
    /// <summary>Stores a token response and returns the opaque exchange code.</summary>
    /// <param name="response">The issued token pair to hand off.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The one-time code, or a failure.</returns>
    Task<Result<string>> StoreAsync(
        AuthTokenResponse response,
        CancellationToken ct = default);

    /// <summary>Consumes an exchange code, removing it from the store.</summary>
    /// <param name="code">The one-time code issued by <see cref="StoreAsync"/>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The stored token response, or a failure when the code is unknown or expired.</returns>
    Task<Result<AuthTokenResponse>> ConsumeAsync(
        string code,
        CancellationToken ct = default);
}

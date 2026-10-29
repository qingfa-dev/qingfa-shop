using Microsoft.AspNetCore.Http;

namespace Application.Common.Integrations.Storages.Security.Guard;

/// <summary>
/// Guards storage operations against repeated anti-forgery failures.
/// Tracks consecutive failures per identity key and blocks further requests
/// when a configurable threshold is reached.
/// </summary>
public interface IStorageAntiForgeryGuard
{
    /// <summary>
    /// Validates the anti-forgery token for the identity and request context.
    /// Steps: check block status → validate CSRF token → record failure on invalid → reset on valid.
    /// Returns <c>Ok</c> on success, <c>Storage.AccessDenied</c> on invalid token,
    /// or <c>Storage.TooManyAttempts</c> when blocked.
    /// </summary>
    Task<Result> ValidateRequestAsync(string identityKey, HttpContext httpContext, CancellationToken ct = default);

    /// <summary>
    /// Records a failed anti-forgery attempt for the given identity.
    /// Returns <see cref="Result.Ok"/> when under the failure threshold,
    /// or a failure result with <c>Storage.TooManyAttempts</c> when the
    /// threshold has been reached or exceeded.
    /// </summary>
    Task<Result> RecordFailureAsync(string identityKey, CancellationToken ct = default);

    /// <summary>
    /// Clears the failure counter for the given identity.
    /// Call this after a successful upload or successful CSRF validation.
    /// </summary>
    Task<Result> ResetAsync(string identityKey, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> when the identity has accumulated too many
    /// consecutive failures and should be blocked.
    /// </summary>
    Task<bool> IsBlockedAsync(string identityKey, CancellationToken ct = default);
}

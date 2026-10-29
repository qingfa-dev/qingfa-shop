using Application.Common.Integrations.Storages.Models;

namespace Application.Common.Integrations.Storages.Security;

/// <summary>
/// Enforces upload security rules before an object reaches the storage provider.
/// Implementations may check MIME type, file size, extension allowlists, magic bytes, etc.
/// </summary>
public interface IStorageSecurityEnforcer
{
    /// <summary>
    /// Validates the <paramref name="request"/> against security policy.
    /// Returns when the request is permitted,
    /// or a failure result with an appropriate error code when it is denied.
    /// </summary>
    /// <param name="request">The upload request to evaluate.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> EnforceAsync(UploadRequest request, CancellationToken ct = default);
}
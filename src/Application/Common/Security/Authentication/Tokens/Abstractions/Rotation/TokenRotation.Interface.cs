namespace Application.Common.Security.Authentication.Tokens.Abstractions.Rotation;

/// <summary>Rotates refresh tokens and detects reuse of already-rotated tokens.</summary>
public interface ITokenRotationService
{
    /// <summary>Rotates a refresh token into a fresh token in the same family.</summary>
    /// <param name="refreshToken">The plaintext token presented by the client.</param>
    /// <param name="nowUtc">The UTC rotation moment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The replacement refresh token, or a failure.</returns>
    Task<Result<RefreshTokenIssue>> RotateAsync(
        string refreshToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}

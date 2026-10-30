using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Models;

/// <summary>The public token pair returned by every successful credential exchange.</summary>
/// <param name="AccessToken">The signed JWT access token.</param>
/// <param name="ExpiresAtUtc">The UTC moment the access token expires.</param>
/// <param name="RefreshToken">The opaque refresh token.</param>
/// <param name="User">The authenticated user projection.</param>
/// <remarks>
/// The application auth response and the persisted refresh-token entity stay internal to
/// this boundary; only the plaintext tokens and the user projection cross the wire.
/// </remarks>
public record AuthTokenModel(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    ApplicationIdentity.IdentityUserModel User);

/// <summary>The token pair handed to the client by the external sign-in exchange.</summary>
/// <param name="AccessToken">The signed JWT access token.</param>
/// <param name="ExpiresAtUtc">The UTC moment the access token expires.</param>
/// <param name="RefreshToken">The opaque refresh token.</param>
/// <param name="User">The authenticated user projection.</param>
public sealed record AuthTokenResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    ApplicationIdentity.IdentityUserModel User)
    : AuthTokenModel(AccessToken, ExpiresAtUtc, RefreshToken, User);

/// <summary>The public projection of the current user's profile.</summary>
public record AuthUserModel
{
    /// <summary>Gets the user identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the username.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Gets the email address.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets the first name.</summary>
    public string? FirstName { get; init; }

    /// <summary>Gets the optional last name.</summary>
    public string? LastName { get; init; }

    /// <summary>Gets a value indicating whether the email address is confirmed.</summary>
    public bool EmailConfirmed { get; init; }

    /// <summary>Gets a value indicating whether two-factor authentication is enabled.</summary>
    public bool TwoFactorEnabled { get; init; }
}

/// <summary>The current user's two-factor state and setup data.</summary>
public record AuthTwoFactorModel
{
    /// <summary>Gets a value indicating whether two-factor authentication is enabled.</summary>
    public bool IsTwoFactorEnabled { get; init; }

    /// <summary>Gets the shared authenticator key, when one exists.</summary>
    public string? SharedKey { get; init; }

    /// <summary>Gets the authenticator setup URI, when one exists.</summary>
    public string? AuthenticatorUri { get; init; }

    /// <summary>Gets the remaining recovery codes.</summary>
    public IReadOnlyCollection<string> RecoveryCodes { get; init; } = [];
}

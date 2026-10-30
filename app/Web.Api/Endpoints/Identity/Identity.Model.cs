using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Web.Api.Endpoints.Identity;

/// <summary>The public token pair returned by every successful credential exchange.</summary>
/// <param name="AccessToken">The signed JWT access token.</param>
/// <param name="ExpiresAtUtc">The UTC moment the access token expires.</param>
/// <param name="RefreshToken">The opaque refresh token.</param>
/// <param name="User">The authenticated user projection.</param>
/// <remarks>
/// The application auth response and the persisted refresh-token entity stay internal to
/// this boundary; only the plaintext tokens and the user projection cross the wire.
/// </remarks>
public sealed record AuthTokenResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    ApplicationIdentity.IdentityUserModel User)
{
}

/// <summary>Registration data for a local account.</summary>
/// <param name="UserName">The requested username.</param>
/// <param name="Email">The email address.</param>
/// <param name="Password">The plain-text password, hashed before storage.</param>
/// <param name="FirstName">The optional first name.</param>
/// <param name="LastName">The optional last name.</param>
public sealed record RegisterRequest(
    string UserName,
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null)
{
    /// <summary>Maps the wire request onto the application contract.</summary>
    public ApplicationIdentity.RegisterRequest ToApplication() =>
        new(UserName, Email, Password, FirstName, LastName);
}

/// <summary>Local sign-in credentials.</summary>
/// <param name="Identifier">The username or email address.</param>
/// <param name="Password">The plain-text password.</param>
/// <param name="TwoFactorCode">The optional authenticator code.</param>
/// <param name="TwoFactorRecoveryCode">The optional recovery code.</param>
public sealed record LoginRequest(
    string Identifier,
    string Password,
    string? TwoFactorCode = null,
    string? TwoFactorRecoveryCode = null)
{
    /// <summary>Maps the wire request onto the application contract.</summary>
    public ApplicationIdentity.LoginRequest ToApplication() =>
        new(Identifier, Password, TwoFactorCode, TwoFactorRecoveryCode);
}

/// <summary>A refresh-token rotation request.</summary>
/// <param name="RefreshToken">The refresh token to rotate.</param>
public sealed record RefreshRequest(string RefreshToken)
{
    /// <summary>Maps the wire request onto the application contract.</summary>
    public ApplicationIdentity.RefreshRequest ToApplication() => new(RefreshToken);
}

/// <summary>A logout request carrying the optional refresh token to revoke.</summary>
/// <param name="RefreshToken">The optional refresh token to revoke.</param>
/// <remarks>
/// The access-token identifier and expiry are read from the authenticated principal, never
/// from the body, so a caller cannot blacklist an arbitrary token.
/// </remarks>
public sealed record LogoutRequest(string? RefreshToken = null)
{
    /// <summary>Maps the wire request and principal claims onto the application contract.</summary>
    public ApplicationIdentity.LogoutRequest ToApplication(
        string? accessTokenJti,
        DateTimeOffset? accessTokenExpiresAtUtc) =>
        new(RefreshToken, accessTokenJti, accessTokenExpiresAtUtc);
}

/// <summary>Email confirmation data.</summary>
/// <param name="UserId">The user identifier from the confirmation link.</param>
/// <param name="Token">The confirmation token.</param>
public sealed record ConfirmEmailRequest(string UserId, string Token)
{
    /// <summary>Maps the wire request onto the application contract.</summary>
    public ApplicationIdentity.ConfirmEmailRequest ToApplication() => new(UserId, Token);
}

/// <summary>A request to resend an email confirmation.</summary>
/// <param name="Email">The email address to resend the confirmation to.</param>
public sealed record ResendConfirmationEmailRequest(string Email)
{
    /// <summary>Maps the wire request onto the application contract.</summary>
    public ApplicationIdentity.ResendConfirmationEmailRequest ToApplication() => new(Email);
}

/// <summary>A forgot-password request.</summary>
/// <param name="Email">The email address to send a reset link to.</param>
public sealed record ForgotPasswordRequest(string Email)
{
    /// <summary>Maps the wire request onto the application contract.</summary>
    public ApplicationIdentity.ForgotPasswordRequest ToApplication() => new(Email);
}

/// <summary>A password reset request.</summary>
/// <param name="Email">The email address being reset.</param>
/// <param name="Token">The reset token.</param>
/// <param name="NewPassword">The replacement password.</param>
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword)
{
    /// <summary>Maps the wire request onto the application contract.</summary>
    public ApplicationIdentity.ResetPasswordRequest ToApplication() => new(Email, Token, NewPassword);
}

/// <summary>Profile update data.</summary>
/// <param name="Email">The optional replacement email address.</param>
/// <param name="NewPassword">The optional replacement password.</param>
/// <param name="OldPassword">The current password, required with a new password.</param>
/// <param name="FirstName">The optional replacement first name.</param>
/// <param name="LastName">The optional replacement last name.</param>
public sealed record UpdateInfoRequest(
    string? Email = null,
    string? NewPassword = null,
    string? OldPassword = null,
    string? FirstName = null,
    string? LastName = null)
{
    /// <summary>Maps the wire request onto the application contract.</summary>
    public ApplicationIdentity.UpdateInfoRequest ToApplication() =>
        new(Email, NewPassword, OldPassword, FirstName, LastName);
}

/// <summary>Two-factor configuration changes.</summary>
/// <param name="Enable">Whether to enable or disable two-factor authentication.</param>
/// <param name="TwoFactorCode">The authenticator code proving the change.</param>
/// <param name="ResetSharedKey">Whether to rotate the shared key.</param>
/// <param name="ResetRecoveryCodes">Whether to rotate the recovery codes.</param>
public sealed record TwoFactorRequest(
    bool? Enable = null,
    string? TwoFactorCode = null,
    bool? ResetSharedKey = null,
    bool? ResetRecoveryCodes = null)
{
    /// <summary>Maps the wire request onto the application contract.</summary>
    public ApplicationIdentity.TwoFactorRequest ToApplication() =>
        new(Enable, TwoFactorCode, ResetSharedKey, ResetRecoveryCodes);
}

/// <summary>Exchanges a one-time external sign-in code for the issued token pair.</summary>
/// <param name="Code">The code delivered to the client redirect URL.</param>
public sealed record ExternalExchangeRequest(string Code);

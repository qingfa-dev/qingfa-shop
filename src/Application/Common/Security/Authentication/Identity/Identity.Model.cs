using Application.Common.Security.Authentication.Tokens;

namespace Application.Common.Security.Authentication.Identity;

/// <summary>A public projection of an identity user.</summary>
/// <param name="Id">The user identifier.</param>
/// <param name="UserName">The username.</param>
/// <param name="Email">The email address.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="LastName">The optional last name.</param>
/// <param name="EmailConfirmed">Whether the email is confirmed.</param>
/// <param name="TwoFactorEnabled">Whether two-factor authentication is enabled.</param>
public sealed record IdentityUserModel(
    Guid Id,
    string UserName,
    string Email,
    string? FirstName,
    string? LastName,
    bool EmailConfirmed,
    bool TwoFactorEnabled);

/// <summary>Registration data for a local identity account.</summary>
public sealed record RegisterRequest(
    string UserName,
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null);

/// <summary>Local sign-in credentials.</summary>
public sealed record LoginRequest(
    string Identifier,
    string Password,
    string? TwoFactorCode = null,
    string? TwoFactorRecoveryCode = null);

/// <summary>A refresh-token rotation request.</summary>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>A logout request for the current refresh token and optional access token.</summary>
public sealed record LogoutRequest(
    string? RefreshToken = null,
    string? AccessTokenJti = null,
    DateTimeOffset? AccessTokenExpiresAtUtc = null);

/// <summary>Email confirmation data.</summary>
public sealed record ConfirmEmailRequest(string UserId, string Token);

/// <summary>A request to resend an email confirmation.</summary>
public sealed record ResendConfirmationEmailRequest(string Email);

/// <summary>A forgot-password request.</summary>
public sealed record ForgotPasswordRequest(string Email);

/// <summary>A password reset request.</summary>
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

/// <summary>Profile update data.</summary>
public sealed record UpdateInfoRequest(
    string? Email = null,
    string? NewPassword = null,
    string? OldPassword = null,
    string? FirstName = null,
    string? LastName = null);

/// <summary>Two-factor configuration changes.</summary>
public sealed record TwoFactorRequest(
    bool? Enable = null,
    string? TwoFactorCode = null,
    bool? ResetSharedKey = null,
    bool? ResetRecoveryCodes = null);

/// <summary>Two-factor state and setup data.</summary>
public sealed record TwoFactorInfo(
    bool IsTwoFactorEnabled,
    string? SharedKey,
    string? AuthenticatorUri,
    IReadOnlyCollection<string> RecoveryCodes);

/// <summary>A configured external authentication provider.</summary>
public sealed record ExternalProviderModel(string Name, string DisplayName);

/// <summary>Normalized external provider identity data.</summary>
public sealed record ExternalUserInfo(
    string Provider,
    string ProviderKey,
    string? DisplayName,
    string? Email);

/// <summary>An issued access/refresh token pair and its user.</summary>
public sealed record AuthResponse(
    AccessToken AccessToken,
    RefreshTokenIssue RefreshToken,
    IdentityUserModel User);

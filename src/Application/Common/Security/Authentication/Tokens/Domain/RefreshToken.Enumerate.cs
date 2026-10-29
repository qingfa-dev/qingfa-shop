namespace Application.Common.Security.Authentication.Tokens.Domain;

/// <summary>Explains why a refresh token was revoked.</summary>
public enum RefreshTokenRevocationReason
{
    /// <summary>The token was rotated into a newer token.</summary>
    Replaced = 1,

    /// <summary>The user signed out of the current session.</summary>
    UserLogout = 2,

    /// <summary>The user signed out of every session.</summary>
    UserLogoutAll = 3,

    /// <summary>The token was reused after rotation, indicating theft.</summary>
    ReuseDetected = 4
}

using Application.BuildingBlocks.Security.Identity.Auth.Models;

namespace Application.BuildingBlocks.Security.Identity.Auth.Abstractions;

/// <summary>Application-level identity operations backed by ASP.NET Core Identity.</summary>
public interface IIdentityService
{
    /// <summary>Registers a local account.</summary>
    Task<Result<IdentityUserModel>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Signs in a local account and issues a token pair.</summary>
    Task<Result<AuthTokenResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Rotates a refresh token and issues a new token pair.</summary>
    Task<Result<AuthTokenResponse>> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes the supplied refresh token and optionally blacklists the access token.</summary>
    Task<Result> LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Confirms an email address.</summary>
    Task<Result> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Resends an email confirmation when the address exists.</summary>
    Task<Result> ResendConfirmationEmailAsync(
        ResendConfirmationEmailRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Starts the forgot-password flow when the address exists.</summary>
    Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Resets a password with a valid token.</summary>
    Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the current user's profile.</summary>
    Task<Result<IdentityUserModel>> GetInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the current user's profile.</summary>
    Task<Result<IdentityUserModel>> UpdateInfoAsync(
        Guid userId,
        UpdateInfoRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the current two-factor state and setup data.</summary>
    Task<Result<TwoFactorInfo>> GetTwoFactorInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Enables, disables, or resets two-factor authentication.</summary>
    Task<Result<TwoFactorInfo>> UpdateTwoFactorAsync(
        Guid userId,
        TwoFactorRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Sends identity confirmation and password-reset messages.</summary>
public interface IIdentityEmailSender
{
    /// <summary>Sends an email confirmation link.</summary>
    Task<Result> SendConfirmationAsync(
        string email,
        string link,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a password-reset link.</summary>
    Task<Result> SendPasswordResetAsync(
        string email,
        string link,
        CancellationToken cancellationToken = default);
}

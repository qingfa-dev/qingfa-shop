using System.Globalization;
using System.Security.Claims;

using Application.Common.Security.Authentication.Contexts;
using Application.Features.Identity.Mappings;
using Application.Features.Identity.Models;

using Core.Common.Monad.Errors;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity;

/// <summary>Orchestrates the local identity use cases without MediatR.</summary>
/// <remarks>
/// The HTTP module owns routing and response writing; this service owns the
/// application calls, the current-user guard, and the public model mapping.
/// </remarks>
public sealed class IdentityFeature(
    ApplicationIdentity.IIdentityService identity,
    IUserContext userContext)
{
    private static readonly long MaxUnixSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

    /// <summary>Registers a local account.</summary>
    public Task<Result<ApplicationIdentity.IdentityUserModel>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
        => identity.RegisterAsync(request.ToApplication(), cancellationToken);

    /// <summary>Signs in and returns the public token pair.</summary>
    public async Task<Result<AuthTokenResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
        => (await identity.LoginAsync(request.ToApplication(), cancellationToken)
            .ConfigureAwait(false))
            .Map(IdentityMapper.ToTokenResponse);

    /// <summary>Rotates a refresh token and returns the public token pair.</summary>
    public async Task<Result<AuthTokenResponse>> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
        => (await identity.RefreshAsync(request.ToApplication(), cancellationToken)
            .ConfigureAwait(false))
            .Map(IdentityMapper.ToTokenResponse);

    /// <summary>Revokes the current refresh token and access token.</summary>
    public async Task<Result> LogoutAsync(
        LogoutRequest? request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return ToNoContent(await identity.LogoutAsync(
            ToLogoutRequest(request, principal),
            cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Confirms an email address.</summary>
    public async Task<Result> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken = default)
        => ToNoContent(await identity.ConfirmEmailAsync(
            request.ToApplication(),
            cancellationToken).ConfigureAwait(false));

    /// <summary>Resends an email confirmation.</summary>
    public async Task<Result> ResendConfirmationAsync(
        ResendConfirmationEmailRequest request,
        CancellationToken cancellationToken = default)
        => ToNoContent(await identity.ResendConfirmationEmailAsync(
            request.ToApplication(),
            cancellationToken).ConfigureAwait(false));

    /// <summary>Starts the forgot-password flow.</summary>
    public async Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
        => ToNoContent(await identity.ForgotPasswordAsync(
            request.ToApplication(),
            cancellationToken).ConfigureAwait(false));

    /// <summary>Resets a password with a valid token.</summary>
    public async Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
        => ToNoContent(await identity.ResetPasswordAsync(
            request.ToApplication(),
            cancellationToken).ConfigureAwait(false));

    /// <summary>Returns the current user's profile.</summary>
    public Task<Result<ApplicationIdentity.IdentityUserModel>> GetMeAsync(
        CancellationToken cancellationToken = default)
        => WithUserAsync(
            (userId, token) => identity.GetInfoAsync(userId, token),
            cancellationToken);

    /// <summary>Updates the current user's profile.</summary>
    public Task<Result<ApplicationIdentity.IdentityUserModel>> UpdateMeAsync(
        UpdateInfoRequest request,
        CancellationToken cancellationToken = default)
        => WithUserAsync(
            (userId, token) => identity.UpdateInfoAsync(userId, request.ToApplication(), token),
            cancellationToken);

    /// <summary>Returns the current user's two-factor state.</summary>
    public Task<Result<ApplicationIdentity.TwoFactorInfo>> GetTwoFactorAsync(
        CancellationToken cancellationToken = default)
        => WithUserAsync(
            (userId, token) => identity.GetTwoFactorInfoAsync(userId, token),
            cancellationToken);

    /// <summary>Updates the current user's two-factor state.</summary>
    public Task<Result<ApplicationIdentity.TwoFactorInfo>> UpdateTwoFactorAsync(
        TwoFactorRequest request,
        CancellationToken cancellationToken = default)
        => WithUserAsync(
            (userId, token) => identity.UpdateTwoFactorAsync(userId, request.ToApplication(), token),
            cancellationToken);

    /// <summary>Resolves the authenticated user identifier or a 401 failure.</summary>
    public Result<Guid> RequireUserId() => RequireUserId(userContext);

    /// <summary>Resolves a user identifier from a request context.</summary>
    public static Result<Guid> RequireUserId(IUserContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Guid.TryParse(context.UserId, out Guid userId)
            ? Result<Guid>.Ok(userId)
            : Result<Guid>.Fail(Error.Unauthorized(
                "Authentication.UserIdMissing",
                "The authenticated user identifier is missing or invalid."));
    }

    /// <summary>Builds the logout command from the body and the authenticated principal.</summary>
    public static ApplicationIdentity.LogoutRequest ToLogoutRequest(
        LogoutRequest? request,
        ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return (request ?? new LogoutRequest()).ToApplication(
            principal.FindFirstValue("jti"),
            ReadAccessTokenExpiry(principal));
    }

    private async Task<Result<T>> WithUserAsync<T>(
        Func<Guid, CancellationToken, Task<Result<T>>> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        Result<Guid> userId = RequireUserId();
        return userId.IsFailure
            ? Result<T>.Fail(userId.Errors)
            : await action(userId.Value, cancellationToken).ConfigureAwait(false);
    }

    private static Result ToNoContent(Result result)
        => result.IsSuccess ? Result.NoContent() : result;

    public static DateTimeOffset? ReadAccessTokenExpiry(ClaimsPrincipal principal)
    {
        string? expiresAt = principal.FindFirstValue("exp");

        if (!long.TryParse(
            expiresAt,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out long seconds))
        {
            return null;
        }

        return seconds >= 0 && seconds <= MaxUnixSeconds
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }
}

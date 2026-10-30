using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Application.Common.Security.Authentication.Contexts;
using Application.Common.Security.Authentication.Identity;

using Carter;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Web.Api.Endpoints.Results;

using ApplicationLogoutRequest = Application.Common.Security.Authentication.Identity.LogoutRequest;

namespace Web.Api.Endpoints.Identity;

/// <summary>Carter module for local authentication and the current user's profile.</summary>
public sealed class IdentityEndpoint : ICarterModule
{
    private const string BasePath = "/api/auth";

    private static readonly long MaxUnixSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BasePath).WithTags("Authentication");

        group.MapPost("/register", RegisterAsync).AllowAnonymous();
        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapPost("/refresh", RefreshAsync).AllowAnonymous();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapPost("/confirm-email", ConfirmEmailAsync).AllowAnonymous();
        group.MapPost("/resend-confirmation", ResendConfirmationAsync).AllowAnonymous();
        group.MapPost("/forgot-password", ForgotPasswordAsync).AllowAnonymous();
        group.MapPost("/reset-password", ResetPasswordAsync).AllowAnonymous();
        group.MapGet("/me", GetMeAsync).RequireAuthorization();
        group.MapPut("/me", UpdateMeAsync).RequireAuthorization();
        group.MapGet("/me/2fa", GetTwoFactorAsync).RequireAuthorization();
        group.MapPut("/me/2fa", UpdateTwoFactorAsync).RequireAuthorization();
    }

    /// <summary>Resolves the authenticated user identifier or a Result-shaped 401.</summary>
    /// <param name="userContext">The current request's user context.</param>
    /// <returns>The parsed user identifier, or an unauthorized failure.</returns>
    internal static Result<Guid> RequireUserId(IUserContext userContext)
    {
        ArgumentNullException.ThrowIfNull(userContext);

        return Guid.TryParse(userContext.UserId, out Guid userId)
            ? Result<Guid>.Ok(userId)
            : Result<Guid>.Fail(Error.Unauthorized(
                "Authentication.UserIdMissing",
                "The authenticated user identifier is missing or invalid."));
    }

    /// <summary>Runs an authenticated action with the resolved user identifier.</summary>
    /// <param name="userContext">The current request's user context.</param>
    /// <param name="action">The action to run when the identifier is valid.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The action result, or a Result-shaped 401 when the identifier is missing.</returns>
    internal static Task<IResult> WithUserIdAsync(
        IUserContext userContext,
        Func<Guid, CancellationToken, Task<IResult>> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        Result<Guid> userId = RequireUserId(userContext);

        return userId.IsFailure
            ? Task.FromResult(userId.ToApiResult())
            : action(userId.Value, cancellationToken);
    }

    /// <summary>Builds the logout command from the body and the authenticated principal.</summary>
    /// <param name="request">The optional body carrying a refresh token.</param>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The application logout request.</returns>
    internal static ApplicationLogoutRequest ToLogoutRequest(
        LogoutRequest? request,
        ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return (request ?? new LogoutRequest()).ToApplication(
            principal.FindFirstValue(JwtRegisteredClaimNames.Jti),
            ReadAccessTokenExpiry(principal));
    }

    /// <summary>Reads the access-token expiry from the principal's <c>exp</c> claim.</summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The expiry, or <c>null</c> when the claim is absent or out of range.</returns>
    internal static DateTimeOffset? ReadAccessTokenExpiry(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string? expiresAt = principal.FindFirstValue(JwtRegisteredClaimNames.Exp);

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

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => (await identity.RegisterAsync(request.ToApplication(), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => (await identity.LoginAsync(request.ToApplication(), cancellationToken))
            .Map(AuthTokenResponse.FromAuthResponse)
            .ToApiResult();

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => (await identity.RefreshAsync(request.ToApplication(), cancellationToken))
            .Map(AuthTokenResponse.FromAuthResponse)
            .ToApiResult();

    private static async Task<IResult> LogoutAsync(
        LogoutRequest? request,
        ClaimsPrincipal principal,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => ToNoContent(await identity.LogoutAsync(
            ToLogoutRequest(request, principal),
            cancellationToken));

    private static async Task<IResult> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => ToNoContent(await identity.ConfirmEmailAsync(
            request.ToApplication(),
            cancellationToken));

    private static async Task<IResult> ResendConfirmationAsync(
        ResendConfirmationEmailRequest request,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => ToNoContent(await identity.ResendConfirmationEmailAsync(
            request.ToApplication(),
            cancellationToken));

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => ToNoContent(await identity.ForgotPasswordAsync(
            request.ToApplication(),
            cancellationToken));

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => ToNoContent(await identity.ResetPasswordAsync(
            request.ToApplication(),
            cancellationToken));

    private static Task<IResult> GetMeAsync(
        IUserContext userContext,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => WithUserIdAsync(
            userContext,
            async (userId, token) => (await identity.GetInfoAsync(userId, token)).ToApiResult(),
            cancellationToken);

    private static Task<IResult> UpdateMeAsync(
        UpdateInfoRequest request,
        IUserContext userContext,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => WithUserIdAsync(
            userContext,
            async (userId, token) => (await identity.UpdateInfoAsync(
                userId,
                request.ToApplication(),
                token)).ToApiResult(),
            cancellationToken);

    private static Task<IResult> GetTwoFactorAsync(
        IUserContext userContext,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => WithUserIdAsync(
            userContext,
            async (userId, token) => (await identity.GetTwoFactorInfoAsync(userId, token)).ToApiResult(),
            cancellationToken);

    private static Task<IResult> UpdateTwoFactorAsync(
        TwoFactorRequest request,
        IUserContext userContext,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => WithUserIdAsync(
            userContext,
            async (userId, token) => (await identity.UpdateTwoFactorAsync(
                userId,
                request.ToApplication(),
                token)).ToApiResult(),
            cancellationToken);

    private static IResult ToNoContent(Result result) =>
        result.IsSuccess ? Result.NoContent().ToApiResult() : result.ToApiResult();
}

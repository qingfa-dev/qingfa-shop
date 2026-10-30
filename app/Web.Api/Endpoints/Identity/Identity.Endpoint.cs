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
    {
        var logout = new ApplicationLogoutRequest(
            request?.RefreshToken,
            principal.FindFirstValue(JwtRegisteredClaimNames.Jti),
            ReadAccessTokenExpiry(principal));

        return ToNoContent(await identity.LogoutAsync(logout, cancellationToken));
    }

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

    private static async Task<IResult> GetMeAsync(
        IUserContext userContext,
        IIdentityService identity,
        CancellationToken cancellationToken)
    {
        Result<Guid> userId = RequireUserId(userContext);

        return userId.IsFailure
            ? userId.ToApiResult()
            : (await identity.GetInfoAsync(userId.Value, cancellationToken)).ToApiResult();
    }

    private static async Task<IResult> UpdateMeAsync(
        UpdateInfoRequest request,
        IUserContext userContext,
        IIdentityService identity,
        CancellationToken cancellationToken)
    {
        Result<Guid> userId = RequireUserId(userContext);

        return userId.IsFailure
            ? userId.ToApiResult()
            : (await identity.UpdateInfoAsync(
                userId.Value,
                request.ToApplication(),
                cancellationToken)).ToApiResult();
    }

    private static async Task<IResult> GetTwoFactorAsync(
        IUserContext userContext,
        IIdentityService identity,
        CancellationToken cancellationToken)
    {
        Result<Guid> userId = RequireUserId(userContext);

        return userId.IsFailure
            ? userId.ToApiResult()
            : (await identity.GetTwoFactorInfoAsync(userId.Value, cancellationToken)).ToApiResult();
    }

    private static async Task<IResult> UpdateTwoFactorAsync(
        TwoFactorRequest request,
        IUserContext userContext,
        IIdentityService identity,
        CancellationToken cancellationToken)
    {
        Result<Guid> userId = RequireUserId(userContext);

        return userId.IsFailure
            ? userId.ToApiResult()
            : (await identity.UpdateTwoFactorAsync(
                userId.Value,
                request.ToApplication(),
                cancellationToken)).ToApiResult();
    }

    private static IResult ToNoContent(Result result) =>
        result.IsSuccess ? Result.NoContent().ToApiResult() : result.ToApiResult();

    private static DateTimeOffset? ReadAccessTokenExpiry(ClaimsPrincipal principal)
    {
        string? expiresAt = principal.FindFirstValue(JwtRegisteredClaimNames.Exp);

        return long.TryParse(
            expiresAt,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out long seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }
}

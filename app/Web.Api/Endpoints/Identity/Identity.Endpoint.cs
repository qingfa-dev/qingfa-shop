using System.Globalization;
using System.Security.Claims;

using Application.Features.Identity.Auth.Commands;
using Application.Features.Identity.Auth.Models;
using Application.Features.Identity.Auth.Queries;
using Application.Features.Identity.External.Commands;
using Application.Features.Identity.External.Mappings;
using Application.Features.Identity.External.Models;
using Application.Features.Identity.External.Queries;

using Carter;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Infrastructure.Integrations.Serialization;

using MediatR;

using Microsoft.AspNetCore.Authentication;

using Web.Api.Endpoints.Identity.Mappings;
using Web.Api.Endpoints.Identity.Models;


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
        group.MapGet("/providers", GetProvidersAsync).AllowAnonymous();
        group.MapGet("/external/{provider}/challenge", ChallengeAsync).AllowAnonymous();
        group.MapGet("/external/{provider}/callback", CallbackAsync).AllowAnonymous();
        group.MapPost("/external/exchange", ExchangeAsync).AllowAnonymous();
    }

    /// <summary>Builds the logout command request from the body and the authenticated principal.</summary>
    /// <param name="body">The optional request body.</param>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The request enriched with the access-token claims.</returns>
    /// <remarks>
    /// The access-token identifier and expiry are always read from the principal, never
    /// from the body, so a caller cannot blacklist an arbitrary token.
    /// </remarks>
    internal static LogoutRequest FromPrincipal(LogoutBody? body, ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return new LogoutRequest(
            body?.RefreshToken,
            principal.FindFirstValue("jti"),
            ReadAccessTokenExpiry(principal));
    }

    /// <summary>Reads the access-token expiry from the standard <c>exp</c> claim.</summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The expiry instant, or <c>null</c> when the claim is absent or invalid.</returns>
    internal static DateTimeOffset? ReadAccessTokenExpiry(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

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

    private static async Task<IResult> GetProvidersAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new GetExternalProviders.UseCase(), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> ChallengeAsync(
        string provider,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        Result<ExternalChallengeModel> challenge = await mediator.Send(
            new ExternalChallenge.UseCase(new ExternalChallenge.Request { Provider = provider }),
            cancellationToken);

        return challenge.IsFailure
            ? challenge.ToApiResult()
            : TypedResults.Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = challenge.Value.RedirectUri
                },
                [challenge.Value.Provider]);
    }

    private static async Task<IResult> CallbackAsync(
        string provider,
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyCollection<ExternalProviderModel>> providers = await mediator.Send(
            new GetExternalProviders.UseCase(),
            cancellationToken);

        if (providers.IsFailure)
            return providers.ToApiResult();

        Result<ExternalProviderModel> resolved = providers.Value.ResolveProvider(provider);

        if (resolved.IsFailure)
            return resolved.ToApiResult();

        AuthenticateResult authentication = await context.AuthenticateAsync(resolved.Value.Name);

        if (!authentication.Succeeded || authentication.Principal is null)
            return FailedExternalLogin();

        Result<ExternalUserInfo> user = ExternalPrincipalMapper.ToExternalUserInfo(
            resolved.Value.Name,
            authentication.Principal);

        if (user.IsFailure)
            return user.ToApiResult();

        Result<ExternalCallbackModel> callback = await mediator.Send(
            new ExternalCallback.UseCase(new ExternalCallback.Request
            {
                Provider = user.Value.Provider,
                ProviderKey = user.Value.ProviderKey,
                DisplayName = user.Value.DisplayName,
                Email = user.Value.Email,
            }),
            cancellationToken);

        return callback.IsSuccess
            ? TypedResults.Redirect(callback.Value.RedirectUrl)
            : callback.ToApiResult();
    }

    private static async Task<IResult> ExchangeAsync(
        ExternalExchangeRequest? request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new ExchangeExternalCode.UseCase(request),
                cancellationToken))
            .ToApiResult();

    private static IResult FailedExternalLogin() =>
        Result.Fail(Error.Unauthorized(
            "Auth.External.LoginFailed",
            "The external sign-in could not be completed."))
            .ToApiResult();

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new RegisterUser.UseCase(request), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new Login.UseCase(request), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new RefreshToken.UseCase(request), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> LogoutAsync(
        LogoutBody? body,
        ClaimsPrincipal principal,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new Logout.UseCase(FromPrincipal(body, principal)),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new ConfirmEmail.UseCase(request), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> ResendConfirmationAsync(
        ResendConfirmationEmailRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new ResendConfirmation.UseCase(request), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new ForgotPassword.UseCase(request), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new ResetPassword.UseCase(request), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetMeAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new GetCurrentUser.UseCase(), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> UpdateMeAsync(
        UpdateInfoRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new UpdateCurrentUser.UseCase(request), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetTwoFactorAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new GetTwoFactor.UseCase(), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> UpdateTwoFactorAsync(
        TwoFactorRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new UpdateTwoFactor.UseCase(request), cancellationToken))
            .ToApiResult();
}

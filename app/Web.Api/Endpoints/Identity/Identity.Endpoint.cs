using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Application.Common.Security.Authentication.Contexts;
using Application.Common.Security.Authentication.Identity;

using Carter;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Microsoft.AspNetCore.Authentication;

using Infrastructure.Integrations.Serialization;

using ApplicationLogoutRequest = Application.Common.Security.Authentication.Identity.LogoutRequest;


using Web.Api.Endpoints.Identity.Mappings;

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
        group.MapGet("/providers", GetProviders).AllowAnonymous();
        group.MapGet("/external/{provider}/challenge", ChallengeAsync).AllowAnonymous();
        group.MapGet("/external/{provider}/callback", CallbackAsync).AllowAnonymous();
        group.MapPost("/external/exchange", ExchangeAsync).AllowAnonymous();
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

    /// <summary>Finds the configured provider matching a route segment.</summary>
    /// <param name="providers">The providers reported by the external authentication service.</param>
    /// <param name="provider">The provider segment from the route.</param>
    /// <returns>The configured provider, or a 404 failure when it is not enabled.</returns>
    internal static Result<ExternalProviderModel> ResolveProvider(
        IReadOnlyCollection<ExternalProviderModel> providers,
        string? provider)
    {
        ArgumentNullException.ThrowIfNull(providers);

        ExternalProviderModel? match = string.IsNullOrWhiteSpace(provider)
            ? null
            : providers.FirstOrDefault(candidate =>
                candidate.Name.Equals(provider, StringComparison.OrdinalIgnoreCase) ||
                IsOpenIdConnectAlias(candidate.Name, provider));

        return match is null
            ? Result<ExternalProviderModel>.Fail(Error.NotFound(
                "Auth.External.ProviderUnsupported",
                "The requested external provider is not configured."))
            : Result<ExternalProviderModel>.Ok(match);
    }

    /// <summary>Builds the route segment and callback path for a provider scheme.</summary>
    /// <param name="scheme">The authentication scheme name.</param>
    /// <returns>The lowercase route segment used by the external routes.</returns>
    internal static string ProviderSegment(string scheme)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheme);

        return scheme.Equals(IdentityConstant.Providers.OpenIdConnect, StringComparison.OrdinalIgnoreCase)
            ? "openid"
            : scheme.ToLowerInvariant();
    }

    /// <summary>Builds the provider callback path the challenge must return to.</summary>
    /// <param name="providerSegment">The lowercase provider route segment.</param>
    /// <returns>The absolute callback path.</returns>
    internal static string CallbackPath(string providerSegment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSegment);

        return $"{IdentityConstant.Routes.External}/{providerSegment}/callback";
    }

    /// <summary>Resolves the callback URL registered with the provider's authentication handler.</summary>
    /// <param name="provider">The configured provider.</param>
    /// <param name="option">The external authentication settings.</param>
    /// <returns>The callback path the handler expects.</returns>
    internal static string CallbackRedirectUri(ExternalProviderModel provider, ExternalAuthOption option)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(option);

        // Fallback: OpenID Connect accepts a configured callback path; the other schemes
        // use the route this API exposes.
        return provider.Name.Equals(IdentityConstant.Providers.OpenIdConnect, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(option.OpenIdConnect.CallbackPath)
            ? option.OpenIdConnect.CallbackPath
            : CallbackPath(ProviderSegment(provider.Name));
    }

    /// <summary>Maps an authenticated external principal onto the application user information.</summary>
    /// <param name="provider">The authentication scheme that produced the principal.</param>
    /// <param name="principal">The external principal.</param>
    /// <returns>The normalized external user, or a 401 failure when no subject claim exists.</returns>
    internal static Result<ExternalUserInfo> ToExternalUserInfo(
        string provider,
        ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string? providerKey = FindFirstClaim(
            principal,
            ClaimTypes.NameIdentifier,
            JwtRegisteredClaimNames.Sub);

        return string.IsNullOrWhiteSpace(providerKey)
            ? Result<ExternalUserInfo>.Fail(Error.Unauthorized(
                "Auth.External.SubjectMissing",
                "The external provider did not return a subject identifier."))
            : Result<ExternalUserInfo>.Ok(new ExternalUserInfo(
                provider,
                providerKey,
                FindFirstClaim(principal, ClaimTypes.Name, "name"),
                FindFirstClaim(principal, ClaimTypes.Email, "email")));
    }

    /// <summary>Appends the one-time code to the configured client redirect URL.</summary>
    /// <param name="successRedirectUrl">The configured client URL.</param>
    /// <param name="code">The one-time code.</param>
    /// <returns>The redirect URL carrying the code.</returns>
    internal static string BuildRedirectUrl(string successRedirectUrl, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        string target = string.IsNullOrWhiteSpace(successRedirectUrl) ? "/" : successRedirectUrl;

        string prefix = target.EndsWith('?') || target.EndsWith('&')
            ? target
            : target.Contains('?', StringComparison.Ordinal)
                ? $"{target}&"
                : $"{target}?";

        return $"{prefix}code={Uri.EscapeDataString(code)}";
    }

    private static string? FindFirstClaim(ClaimsPrincipal principal, params string[] claimTypes) =>
        claimTypes
            .Select(principal.FindFirstValue)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static bool IsOpenIdConnectAlias(string scheme, string provider) =>
        scheme.Equals(IdentityConstant.Providers.OpenIdConnect, StringComparison.OrdinalIgnoreCase) &&
        (provider.Equals("openid", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("openidconnect", StringComparison.OrdinalIgnoreCase));

    private static IResult GetProviders(IExternalAuthenticationService external) =>
        Result<IReadOnlyCollection<ExternalProviderModel>>
            .Ok(external.GetProviders())
            .ToApiResult();

    private static IResult ChallengeAsync(
        string provider,
        IExternalAuthenticationService external,
        ExternalAuthOption option)
    {
        Result<ExternalProviderModel> resolved = ResolveProvider(external.GetProviders(), provider);

        return resolved.IsFailure
            ? resolved.ToApiResult()
            : TypedResults.Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = CallbackRedirectUri(resolved.Value, option)
                },
                [resolved.Value.Name]);
    }

    private static async Task<IResult> CallbackAsync(
        string provider,
        HttpContext context,
        IExternalAuthenticationService external,
        ExternalCodeStore codes,
        ExternalAuthOption option,
        CancellationToken cancellationToken)
    {
        Result<ExternalProviderModel> resolved = ResolveProvider(external.GetProviders(), provider);

        if (resolved.IsFailure)
            return resolved.ToApiResult();

        AuthenticateResult authentication = await context.AuthenticateAsync(resolved.Value.Name);

        if (!authentication.Succeeded || authentication.Principal is null)
            return FailedExternalLogin();

        Result<ExternalUserInfo> user = ToExternalUserInfo(
            resolved.Value.Name,
            authentication.Principal);

        if (user.IsFailure)
            return user.ToApiResult();

        Result<AuthTokenResponse> token = (await external.LoginAsync(
                user.Value,
                cancellationToken))
            .Map(IdentityMapper.ToTokenResponse);

        if (token.IsFailure)
            return token.ToApiResult();

        Result<string> code = await codes.StoreAsync(token.Value, cancellationToken);

        return code.IsSuccess
            ? TypedResults.Redirect(BuildRedirectUrl(option.SuccessRedirectUrl, code.Value))
            : code.ToResult().ToApiResult();
    }

    private static async Task<IResult> ExchangeAsync(
        ExternalExchangeRequest? request,
        ExternalCodeStore codes,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Code))
            return Result<AuthTokenResponse>.Fail(Error.Unauthorized(
                "Auth.External.CodeInvalid",
                "The external sign-in code is invalid or expired."))
                .ToApiResult();

        return (await codes.ConsumeAsync(request.Code, cancellationToken)).ToApiResult();
    }

    private static IResult FailedExternalLogin() =>
        Result.Fail(Error.Unauthorized(
            "Auth.External.LoginFailed",
            "The external sign-in could not be completed."))
            .ToApiResult();

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
            .Map(IdentityMapper.ToTokenResponse)
            .ToApiResult();

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        IIdentityService identity,
        CancellationToken cancellationToken)
        => (await identity.RefreshAsync(request.ToApplication(), cancellationToken))
            .Map(IdentityMapper.ToTokenResponse)
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

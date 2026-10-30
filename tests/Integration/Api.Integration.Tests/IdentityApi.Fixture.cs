using System.Security.Claims;
using System.Text.Encodings.Web;

using Application.Features.Identity.Auth;
using Application.Features.Identity.Auth.Abstractions;
using Application.Features.Identity.Auth.Models;
using Application.Features.Identity.Auth.Validators;
using Application.Features.Identity.External.Abstractions;
using Application.Features.Identity.External.Models;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Testing.Fixtures;

namespace Api.Integration.Tests;

/// <summary>Boots the Api with stub identity and external services so route behavior is deterministic.</summary>
public sealed class IdentityApiFactory : ShopApiFactory
{
    /// <summary>The stub identity service installed into the host.</summary>
    public StubIdentityService Identity { get; } = new();

    /// <summary>The stub external authentication service installed into the host.</summary>
    public StubExternalAuthenticationService External { get; } = new();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IIdentityService>();
            services.AddSingleton<IIdentityService>(Identity);

            services.RemoveAll<IExternalAuthenticationService>();
            services.AddSingleton<IExternalAuthenticationService>(External);

            // Initialize: the fake Google scheme stands in for the OAuth handler, so the
            // challenge and callback routes can be exercised without provider secrets.
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, StubExternalAuthenticationHandler>(
                    IdentityConstant.Providers.Google,
                    _ => { });

            External.LoginHandler = _ => Result<AuthTokenResponse>.Ok(Identity.CreateAuthResponse());
        });
    }
}

/// <summary>Records identity calls and returns configurable results without persistence.</summary>
public sealed class StubIdentityService : IIdentityService
{
    private AuthTokenResponse _authResponse = null!;

    /// <summary>Initializes the default success handlers.</summary>
    public StubIdentityService() => Reset();

    /// <summary>The user projection returned by profile operations.</summary>
    public IdentityUserModel User { get; } = new(
        Guid.NewGuid(),
        "admin",
        "admin@example.com",
        "Admin",
        null,
        true,
        false);

    /// <summary>The two-factor projection returned by two-factor operations.</summary>
    public TwoFactorInfo TwoFactor { get; } = new(
        true,
        "shared-key",
        "otpauth://totp/QingFa:admin",
        ["recovery-code"]);

    /// <summary>The last logout command received.</summary>
    public LogoutRequest? LastLogout { get; private set; }

    /// <summary>The last profile update received.</summary>
    public UpdateInfoRequest? LastUpdateInfo { get; private set; }

    /// <summary>The last two-factor update received.</summary>
    public TwoFactorRequest? LastTwoFactor { get; private set; }

    /// <summary>Overrides the registration result after shared validation.</summary>
    public Func<RegisterRequest, Result<IdentityUserModel>> RegisterHandler { get; set; } = null!;

    /// <summary>Overrides the login result after shared validation.</summary>
    public Func<LoginRequest, Result<AuthTokenResponse>> LoginHandler { get; set; } = null!;

    /// <summary>Overrides the refresh result.</summary>
    public Func<RefreshRequest, Result<AuthTokenResponse>> RefreshHandler { get; set; } = null!;

    /// <summary>Overrides the logout result.</summary>
    public Func<LogoutRequest, Result> LogoutHandler { get; set; } = null!;

    /// <summary>Overrides the confirmation result.</summary>
    public Func<ConfirmEmailRequest, Result> ConfirmEmailHandler { get; set; } = null!;

    /// <summary>Overrides the confirmation-resend result.</summary>
    public Func<ResendConfirmationEmailRequest, Result> ResendConfirmationHandler { get; set; } = null!;

    /// <summary>Overrides the forgot-password result.</summary>
    public Func<ForgotPasswordRequest, Result> ForgotPasswordHandler { get; set; } = null!;

    /// <summary>Overrides the reset-password result.</summary>
    public Func<ResetPasswordRequest, Result> ResetPasswordHandler { get; set; } = null!;

    /// <summary>Restores the default success and validation handlers.</summary>
    public void Reset()
    {
        _authResponse = CreateAuthResponse();
        LastLogout = null;
        LastUpdateInfo = null;
        LastTwoFactor = null;

        RegisterHandler = _ => Result<IdentityUserModel>.Ok(User);
        LoginHandler = _ => Result<AuthTokenResponse>.Ok(_authResponse);
        RefreshHandler = _ => Result<AuthTokenResponse>.Ok(_authResponse);
        LogoutHandler = request =>
        {
            LastLogout = request;
            return Result.Ok();
        };
        ConfirmEmailHandler = _ => Result.Ok();
        ResendConfirmationHandler = _ => Result.Ok();
        ForgotPasswordHandler = _ => Result.Ok();
        ResetPasswordHandler = _ => Result.Ok();
    }

    /// <inheritdoc />
    public Task<Result<IdentityUserModel>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<RegisterRequest> validation = IdentityValidator.Validate(request);
        return Task.FromResult(validation.IsFailure
            ? Result<IdentityUserModel>.Fail(validation.Errors)
            : RegisterHandler(request));
    }

    /// <inheritdoc />
    public Task<Result<AuthTokenResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<LoginRequest> validation = IdentityValidator.Validate(request);
        return Task.FromResult(validation.IsFailure
            ? Result<AuthTokenResponse>.Fail(validation.Errors)
            : LoginHandler(request));
    }

    /// <inheritdoc />
    public Task<Result<AuthTokenResponse>> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<RefreshRequest> validation = IdentityValidator.Validate(request);
        return Task.FromResult(validation.IsFailure
            ? Result<AuthTokenResponse>.Fail(validation.Errors)
            : RefreshHandler(request));
    }

    /// <inheritdoc />
    public Task<Result> LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(LogoutHandler(request));

    /// <inheritdoc />
    public Task<Result> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<ConfirmEmailRequest> validation = IdentityValidator.Validate(request);
        return Task.FromResult(validation.IsFailure
            ? Result.Fail(validation.Errors)
            : ConfirmEmailHandler(request));
    }

    /// <inheritdoc />
    public Task<Result> ResendConfirmationEmailAsync(
        ResendConfirmationEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<ResendConfirmationEmailRequest> validation = IdentityValidator.Validate(request);
        return Task.FromResult(validation.IsFailure
            ? Result.Fail(validation.Errors)
            : ResendConfirmationHandler(request));
    }

    /// <inheritdoc />
    public Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<ForgotPasswordRequest> validation = IdentityValidator.Validate(request);
        return Task.FromResult(validation.IsFailure
            ? Result.Fail(validation.Errors)
            : ForgotPasswordHandler(request));
    }

    /// <inheritdoc />
    public Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<ResetPasswordRequest> validation = IdentityValidator.Validate(request);
        return Task.FromResult(validation.IsFailure
            ? Result.Fail(validation.Errors)
            : ResetPasswordHandler(request));
    }

    /// <inheritdoc />
    public Task<Result<IdentityUserModel>> GetInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Result<IdentityUserModel>.Ok(User));

    /// <inheritdoc />
    public Task<Result<IdentityUserModel>> UpdateInfoAsync(
        Guid userId,
        UpdateInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<UpdateInfoRequest> validation = IdentityValidator.Validate(request);
        if (validation.IsFailure)
            return Task.FromResult(Result<IdentityUserModel>.Fail(validation.Errors));

        LastUpdateInfo = request;
        return Task.FromResult(Result<IdentityUserModel>.Ok(User));
    }

    /// <inheritdoc />
    public Task<Result<TwoFactorInfo>> GetTwoFactorInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Result<TwoFactorInfo>.Ok(TwoFactor));

    /// <inheritdoc />
    public Task<Result<TwoFactorInfo>> UpdateTwoFactorAsync(
        Guid userId,
        TwoFactorRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<TwoFactorRequest> validation = IdentityValidator.Validate(request);
        if (validation.IsFailure)
            return Task.FromResult(Result<TwoFactorInfo>.Fail(validation.Errors));

        LastTwoFactor = request;
        return Task.FromResult(Result<TwoFactorInfo>.Ok(TwoFactor));
    }

    /// <summary>Creates the token pair returned by stub login handlers.</summary>
    /// <returns>A token pair bound to <see cref="User"/>.</returns>
    public AuthTokenResponse CreateAuthResponse()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new AuthTokenResponse(
            "access-token",
            now.AddMinutes(15),
            "refresh-token",
            User);
    }
}

/// <summary>Records external login calls and returns a configurable token pair.</summary>
public sealed class StubExternalAuthenticationService : IExternalAuthenticationService
{
    /// <summary>The providers reported to the API; defaults to a single Google provider.</summary>
    public IReadOnlyCollection<ExternalProviderModel> Providers { get; set; } =
        [new ExternalProviderModel(IdentityConstant.Providers.Google, "Google")];

    /// <summary>The normalized user passed to the last login call.</summary>
    public ExternalUserInfo? LastUser { get; private set; }

    /// <summary>Overrides the external login result.</summary>
    public Func<ExternalUserInfo, Result<AuthTokenResponse>> LoginHandler { get; set; } =
        _ => Result<AuthTokenResponse>.Fail(Error.Unexpected(
            "Test.NotConfigured",
            "The external login handler was not configured."));

    /// <inheritdoc />
    public IReadOnlyCollection<ExternalProviderModel> GetProviders() => Providers;

    /// <inheritdoc />
    public Task<Result<AuthTokenResponse>> LoginAsync(
        ExternalUserInfo user,
        CancellationToken cancellationToken = default)
    {
        LastUser = user;
        return Task.FromResult(LoginHandler(user));
    }

    /// <summary>Restores the provider list and the successful login handler.</summary>
    /// <param name="response">The token pair returned by the default handler.</param>
    public void Reset(AuthTokenResponse response)
    {
        Providers = [new ExternalProviderModel(IdentityConstant.Providers.Google, "Google")];
        LastUser = null;
        LoginHandler = _ => Result<AuthTokenResponse>.Ok(response);
    }
}

/// <summary>Stands in for an OAuth handler so the challenge and callback routes run without secrets.</summary>
public sealed class StubExternalAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Handle: the callback test toggles failure through the query string so the API
        // can be proven to map rejected provider callbacks onto a 401 Result.
        if (Context.Request.Query.ContainsKey("fail"))
            return Task.FromResult(AuthenticateResult.Fail("The stub provider rejected the callback."));

        Claim[] claims = Context.Request.Query.ContainsKey("nosub")
            ? [new Claim(ClaimTypes.Email, "external@example.com")]
            :
            [
                new Claim(ClaimTypes.NameIdentifier, "google-user-1"),
                new Claim(ClaimTypes.Name, "External User"),
                new Claim(ClaimTypes.Email, "external@example.com")
            ];

        ClaimsIdentity identity = new(claims, Scheme.Name);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <inheritdoc />
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect("https://provider.example/authorize");
        return Task.CompletedTask;
    }
}

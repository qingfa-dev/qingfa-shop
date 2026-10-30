using Application.Common.Security.Authentication.Identity;
using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Domain;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Testing.Fixtures;

namespace Api.Integration.Tests;

/// <summary>Boots the Api with a stub identity service so route behavior is deterministic.</summary>
public sealed class IdentityApiFactory : ShopApiFactory
{
    /// <summary>The stub identity service installed into the host.</summary>
    public StubIdentityService Identity { get; } = new();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IIdentityService>();
            services.AddSingleton<IIdentityService>(Identity);
        });
    }
}

/// <summary>Records identity calls and returns configurable results without persistence.</summary>
public sealed class StubIdentityService : IIdentityService
{
    private AuthResponse _authResponse = null!;

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

    /// <summary>Overrides the registration result; defaults to password-length validation.</summary>
    public Func<RegisterRequest, Result<IdentityUserModel>> RegisterHandler { get; set; } = null!;

    /// <summary>Overrides the login result; defaults to required-credential validation.</summary>
    public Func<LoginRequest, Result<AuthResponse>> LoginHandler { get; set; } = null!;

    /// <summary>Overrides the refresh result.</summary>
    public Func<RefreshRequest, Result<AuthResponse>> RefreshHandler { get; set; } = null!;

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

        RegisterHandler = request =>
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8
                ? Result<IdentityUserModel>.Fail(Error.UnprocessableEntity(
                    "Identity.Password.Invalid",
                    "The password is too short."))
                : Result<IdentityUserModel>.Ok(User);
        LoginHandler = request =>
            string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password)
                ? Result<AuthResponse>.Fail(Error.BadRequest(
                    "Identity.Credentials.Invalid",
                    "The supplied credentials are invalid."))
                : Result<AuthResponse>.Ok(_authResponse);
        RefreshHandler = _ => Result<AuthResponse>.Ok(_authResponse);
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
        => Task.FromResult(RegisterHandler(request));

    /// <inheritdoc />
    public Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(LoginHandler(request));

    /// <inheritdoc />
    public Task<Result<AuthResponse>> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(RefreshHandler(request));

    /// <inheritdoc />
    public Task<Result> LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(LogoutHandler(request));

    /// <inheritdoc />
    public Task<Result> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ConfirmEmailHandler(request));

    /// <inheritdoc />
    public Task<Result> ResendConfirmationEmailAsync(
        ResendConfirmationEmailRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ResendConfirmationHandler(request));

    /// <inheritdoc />
    public Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ForgotPasswordHandler(request));

    /// <inheritdoc />
    public Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ResetPasswordHandler(request));

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
        LastTwoFactor = request;
        return Task.FromResult(Result<TwoFactorInfo>.Ok(TwoFactor));
    }

    private AuthResponse CreateAuthResponse()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Result<RefreshToken> created = RefreshToken.Create(
            Guid.NewGuid(),
            "token-hash",
            User.Id.ToString(),
            Guid.NewGuid(),
            now,
            now.AddDays(7));

        if (created.IsFailure)
            throw new InvalidOperationException(created.Errors[0].Message);

        return new AuthResponse(
            new AccessToken(
                "access-token",
                "Bearer",
                now,
                now.AddMinutes(15),
                Guid.NewGuid().ToString("N")),
            new RefreshTokenIssue("refresh-token", created.Value),
            User);
    }
}

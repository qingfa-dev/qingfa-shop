using System.IdentityModel.Tokens.Jwt;

using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Security.Authentication.Tokens.Abstractions.Blacklist;
using Application.Features.Identity.Auth;
using Application.Features.Identity.Auth.Abstractions;
using Application.Features.Identity.Auth.Models;
using Application.Features.Identity.External.Abstractions;
using Application.Features.Identity.External.Models;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;
using Infrastructure.Security.Authentication;
using Infrastructure.Security.Identity.Domain.Roles;
using Infrastructure.Security.Identity.Domain.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Unit.Tests.Security.Identity.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "IdentityApi")]
public sealed class IdentityServiceTest
{
    private const string Password = "StrongPass1!ab";

    [Fact(DisplayName = "Register and login should issue a token pair")]
    public async Task RegisterAndLogin_ShouldIssueTokens()
    {
        await using TestHost host = await TestHost.CreateAsync();

        Result<IdentityUserModel> registered = await host.Identity.RegisterAsync(
            new RegisterRequest("alice", "alice@example.com", Password, "Alice", "Smith"));

        registered.IsSuccess.Should().BeTrue();
        registered.Value.Email.Should().Be("alice@example.com");
        host.Email.LastConfirmationLink.Should().NotBeNull();

        Result<AuthTokenResponse> login = await host.Identity.LoginAsync(
            new LoginRequest("alice", Password));

        login.IsSuccess.Should().BeTrue();
        login.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        login.Value.User.UserName.Should().Be("alice");
    }

    [Fact(DisplayName = "Shared validation should drive the auth status codes")]
    public async Task Validation_ShouldDriveStatusCodes()
    {
        await using TestHost host = await TestHost.CreateAsync();

        Result<IdentityUserModel> weak = await host.Identity.RegisterAsync(
            new RegisterRequest("alice", "alice@example.com", "short"));

        weak.StatusCode.Should().Be(400);
        weak.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Password.Invalid");

        Result<AuthTokenResponse> empty = await host.Identity.LoginAsync(
            new LoginRequest("alice", string.Empty));

        empty.StatusCode.Should().Be(401);
        empty.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Credentials.Invalid");
    }

    [Fact(DisplayName = "Refresh should rotate the refresh token")]
    public async Task Refresh_ShouldRotateToken()
    {
        await using TestHost host = await TestHost.CreateAsync();
        await host.Identity.RegisterAsync(
            new RegisterRequest("alice", "alice@example.com", Password, "Alice", "Smith"));
        Result<AuthTokenResponse> login = await host.Identity.LoginAsync(
            new LoginRequest("alice", Password));

        Result<AuthTokenResponse> refreshed = await host.Identity.RefreshAsync(
            new RefreshRequest(login.Value.RefreshToken));

        refreshed.IsSuccess.Should().BeTrue();
        refreshed.Value.RefreshToken.Should().NotBe(login.Value.RefreshToken);
        refreshed.Value.AccessToken.Should().NotBe(login.Value.AccessToken);
    }

    [Fact(DisplayName = "Confirmation and password reset should use Identity tokens")]
    public async Task ConfirmationAndReset_ShouldComplete()
    {
        await using TestHost host = await TestHost.CreateAsync();
        await host.Identity.RegisterAsync(
            new RegisterRequest("alice", "alice@example.com", Password, "Alice", "Smith"));

        string confirmationToken = ExtractToken(host.Email.LastConfirmationLink!);
        string userId = (await host.Users.FindByNameAsync("alice"))!.Id.ToString();
        Result confirmed = await host.Identity.ConfirmEmailAsync(
            new ConfirmEmailRequest(userId, confirmationToken));

        confirmed.IsSuccess.Should().BeTrue();
        (await host.Users.FindByNameAsync("alice"))!.EmailConfirmed.Should().BeTrue();

        await host.Identity.ForgotPasswordAsync(new ForgotPasswordRequest("alice@example.com"));
        string resetToken = ExtractToken(host.Email.LastResetLink!);
        Result reset = await host.Identity.ResetPasswordAsync(
            new ResetPasswordRequest("alice@example.com", resetToken, "AnotherPass1!ab"));

        reset.IsSuccess.Should().BeTrue();
        (await host.Identity.LoginAsync(new LoginRequest("alice", "AnotherPass1!ab")))
            .IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Profile and two-factor state should be manageable")]
    public async Task ProfileAndTwoFactor_ShouldBeManageable()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<IdentityUserModel> registered = await host.Identity.RegisterAsync(
            new RegisterRequest("alice", "alice@example.com", Password, "Alice", "Smith"));

        Result<IdentityUserModel> updated = await host.Identity.UpdateInfoAsync(
            registered.Value.Id,
            new UpdateInfoRequest(FirstName: "Alicia"));

        updated.IsSuccess.Should().BeTrue();
        updated.Value.FirstName.Should().Be("Alicia");

        Result<TwoFactorInfo> info = await host.Identity.GetTwoFactorInfoAsync(registered.Value.Id);
        info.IsSuccess.Should().BeTrue();
        info.Value.SharedKey.Should().NotBeNullOrWhiteSpace();
        info.Value.AuthenticatorUri.Should().StartWith("otpauth://");

        Result<TwoFactorInfo> disabled = await host.Identity.UpdateTwoFactorAsync(
            registered.Value.Id,
            new TwoFactorRequest(Enable: false, ResetRecoveryCodes: true));

        disabled.IsSuccess.Should().BeTrue();
        disabled.Value.IsTwoFactorEnabled.Should().BeFalse();
        disabled.Value.RecoveryCodes.Should().NotBeEmpty();

        Result<TwoFactorInfo> invalid = await host.Identity.UpdateTwoFactorAsync(
            registered.Value.Id,
            new TwoFactorRequest(Enable: true, TwoFactorCode: "000000"));

        invalid.IsFailure.Should().BeTrue();
        invalid.Errors.Should().ContainSingle(
            error => error.Code == "Identity.TwoFactor.Invalid");
    }

    [Fact(DisplayName = "Login should reject unknown users, bad passwords, and inactive accounts")]
    public async Task Login_ShouldRejectInvalidState()
    {
        await using TestHost host = await TestHost.CreateAsync();
        await host.Identity.RegisterAsync(
            new RegisterRequest("alice", "alice@example.com", Password, "Alice", "Smith"));

        (await host.Identity.LoginAsync(new LoginRequest("missing", Password)))
            .Errors[0].Code.Should().Be("Identity.Credentials.Invalid");
        (await host.Identity.LoginAsync(new LoginRequest("alice", "WrongPass1!ab")))
            .Errors[0].Code.Should().Be("Identity.Credentials.Invalid");

        User user = (await host.Users.FindByNameAsync("alice"))!;
        Result<User>.Success(user).Disable();

        (await host.Identity.LoginAsync(new LoginRequest("alice", Password)))
            .Errors[0].Code.Should().Be("Identity.User.Inactive");
    }

    [Fact(DisplayName = "Logout should revoke the refresh token and blacklist the access token")]
    public async Task Logout_ShouldRevokeAndBlacklist()
    {
        await using TestHost host = await TestHost.CreateAsync();
        await host.Identity.RegisterAsync(
            new RegisterRequest("alice", "alice@example.com", Password, "Alice", "Smith"));
        Result<AuthTokenResponse> login = await host.Identity.LoginAsync(new LoginRequest("alice", Password));

        string jti = ReadJti(login.Value.AccessToken);
        Result logout = await host.Identity.LogoutAsync(new LogoutRequest(
            login.Value.RefreshToken,
            jti,
            login.Value.ExpiresAtUtc));

        logout.IsSuccess.Should().BeTrue();
        (await host.Identity.RefreshAsync(new RefreshRequest(login.Value.RefreshToken)))
            .IsFailure.Should().BeTrue();

        var blacklist = host.Scope.ServiceProvider.GetRequiredService<ITokenBlacklist>();
        (await blacklist.ContainsAsync(jti)).Value.Should().BeTrue();
    }

    [Fact(DisplayName = "Forgot and resend should be enumeration-safe for unknown addresses")]
    public async Task RecoveryFlows_ShouldBeEnumerationSafe()
    {
        await using TestHost host = await TestHost.CreateAsync();

        (await host.Identity.ForgotPasswordAsync(new ForgotPasswordRequest("missing@example.com")))
            .IsSuccess.Should().BeTrue();
        (await host.Identity.ResendConfirmationEmailAsync(
            new ResendConfirmationEmailRequest("missing@example.com")))
            .IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Password changes and invalid tokens should be handled")]
    public async Task PasswordChangeAndInvalidTokens_ShouldBeHandled()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<IdentityUserModel> registered = await host.Identity.RegisterAsync(
            new RegisterRequest("alice", "alice@example.com", Password, "Alice", "Smith"));

        Result updated = await host.Identity.UpdateInfoAsync(
            registered.Value.Id,
            new UpdateInfoRequest(NewPassword: "AnotherPass1!ab", OldPassword: Password));

        updated.IsSuccess.Should().BeTrue();
        (await host.Identity.LoginAsync(new LoginRequest("alice", "AnotherPass1!ab")))
            .IsSuccess.Should().BeTrue();

        (await host.Identity.ConfirmEmailAsync(
            new ConfirmEmailRequest(registered.Value.Id.ToString(), "invalid")))
            .IsFailure.Should().BeTrue();
        (await host.Identity.RefreshAsync(new RefreshRequest("invalid"))).IsFailure.Should().BeTrue();
        (await host.Identity.GetInfoAsync(Guid.NewGuid())).IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "External providers should create and relink local accounts")]
    public async Task ExternalLogin_ShouldCreateAndRelinkAccount()
    {
        await using TestHost host = await TestHost.CreateAsync();
        host.External.GetProviders().Select(provider => provider.Name)
            .Should().Contain(IdentityConstant.Providers.Google);

        var external = new ExternalUserInfo(
            IdentityConstant.Providers.Google,
            "google-key",
            "Alice",
            "alice@example.com");

        Result<AuthTokenResponse> first = await host.External.LoginAsync(external);
        Result<AuthTokenResponse> second = await host.External.LoginAsync(external);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        first.Value.User.Email.Should().Be("alice@example.com");
        second.Value.User.Id.Should().Be(first.Value.User.Id);
        (await host.Users.FindByLoginAsync(external.Provider, external.ProviderKey))
            .Should().NotBeNull();
    }

    [Fact(DisplayName = "An unconfigured external provider should be rejected")]
    public async Task ExternalLogin_ShouldRejectUnknownProvider()
    {
        await using TestHost host = await TestHost.CreateAsync();

        Result<AuthTokenResponse> result = await host.External.LoginAsync(
            new ExternalUserInfo("Unknown", "key", null, "user@example.com"));

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Identity.External.ProviderUnsupported");
    }

    private static string ReadJti(string accessToken) =>
        new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Id;

    private static string ExtractToken(string link)
    {
        var query = QueryHelpers.ParseQuery(new Uri(link).Query);
        return query["token"].ToString();
    }

    private sealed class TestHost : IAsyncDisposable
    {
        private TestHost(
            ServiceProvider provider,
            IServiceScope scope,
            FakeEmailSender email,
            IIdentityService identity,
            IExternalAuthenticationService external,
            UserManager<User> users)
        {
            Provider = provider;
            Scope = scope;
            Email = email;
            Identity = identity;
            External = external;
            Users = users;
        }

        public ServiceProvider Provider { get; }

        public IServiceScope Scope { get; }

        public FakeEmailSender Email { get; }

        public IIdentityService Identity { get; }

        public IExternalAuthenticationService External { get; }

        public UserManager<User> Users { get; }

        public static async Task<TestHost> CreateAsync()
        {
            var email = new FakeEmailSender();
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            IServiceCollection services = builder.Services;
            services.AddLogging();
            services.AddSingleton<IIdentityEmailSender>(email);
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"IdentityService_{Guid.NewGuid():N}"));
            services.AddScoped<DbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
            builder.AddAuthentication(BuildConfiguration());

            ServiceProvider provider = services.BuildServiceProvider();
            IServiceScope scope = provider.CreateScope();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            await roles.CreateAsync(Role.Create(IdentityConstant.Defaults.DefaultRole).Value);

            return new TestHost(
                provider,
                scope,
                email,
                scope.ServiceProvider.GetRequiredService<IIdentityService>(),
                scope.ServiceProvider.GetRequiredService<IExternalAuthenticationService>(),
                scope.ServiceProvider.GetRequiredService<UserManager<User>>());
        }

        public ValueTask DisposeAsync()
        {
            Scope.Dispose();
            return Provider.DisposeAsync();
        }

        private static IConfiguration BuildConfiguration() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "qingfa-scaffold-only-signing-key-change-me-32+",
                    ["Jwt:Issuer"] = "QingFa.Shop",
                    ["Jwt:Audience"] = "QingFa.Shop.Clients",
                    ["IdentityApi:PublicBaseUrl"] = "https://tests.local",
                    ["ExternalAuth:Google:Enabled"] = "true",
                    ["ExternalAuth:Google:ClientId"] = "client",
                    ["ExternalAuth:Google:ClientSecret"] = "secret"
                })
                .Build();
    }

    private sealed class FakeEmailSender : IIdentityEmailSender
    {
        public string? LastConfirmationLink { get; private set; }

        public string? LastResetLink { get; private set; }

        public Task<Result> SendConfirmationAsync(
            string email,
            string link,
            CancellationToken cancellationToken = default)
        {
            LastConfirmationLink = link;
            return Task.FromResult(Result.Ok());
        }

        public Task<Result> SendPasswordResetAsync(
            string email,
            string link,
            CancellationToken cancellationToken = default)
        {
            LastResetLink = link;
            return Task.FromResult(Result.Ok());
        }
    }
}

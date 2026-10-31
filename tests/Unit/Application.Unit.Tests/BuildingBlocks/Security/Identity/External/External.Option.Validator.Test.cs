using Application.BuildingBlocks.Security.Identity.External.Options;

using FluentValidation.TestHelper;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.External;

/// <summary>Unit tests for <see cref="ExternalOptionValidator"/>.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "ExternalAuthentication")]
public sealed class ExternalOptionValidatorTest
{
    private readonly ExternalOptionValidator _sut = new();

    [Fact(DisplayName = "External options should accept the defaults")]
    public void Defaults_ShouldPass()
    {
        TestValidationResult<ExternalAuthOption> result = _sut.TestValidate(new ExternalAuthOption());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory(DisplayName = "External options should require a success redirect URL")]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankSuccessRedirectUrl_ShouldFail(string successRedirectUrl)
    {
        ExternalAuthOption options = new() { SuccessRedirectUrl = successRedirectUrl };

        TestValidationResult<ExternalAuthOption> result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.SuccessRedirectUrl)
            .WithErrorCode(ExternalOptionOutcome.Failure.SuccessRedirectUrlRequired.Code);
    }

    [Theory(DisplayName = "External options should bound the code lifetime")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3601)]
    public void CodeLifetimeOutOfRange_ShouldFail(int lifetimeSeconds)
    {
        ExternalAuthOption options = new() { CodeLifetimeSeconds = lifetimeSeconds };

        TestValidationResult<ExternalAuthOption> result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.CodeLifetimeSeconds)
            .WithErrorCode(ExternalOptionOutcome.Failure.CodeLifetimeSecondsOutOfRange.Code);
    }

    [Fact(DisplayName = "External options should accept the code lifetime boundaries")]
    public void CodeLifetimeBoundaries_ShouldPass()
    {
        ExternalAuthOption minimum = new()
        {
            CodeLifetimeSeconds = ExternalAuthConstant.Constraints.CodeLifetimeSecondsMin
        };
        ExternalAuthOption maximum = new()
        {
            CodeLifetimeSeconds = ExternalAuthConstant.Constraints.CodeLifetimeSecondsMax
        };

        _sut.TestValidate(minimum).ShouldNotHaveAnyValidationErrors();
        _sut.TestValidate(maximum).ShouldNotHaveAnyValidationErrors();
    }

    [Theory(DisplayName = "Enabled providers should require a client identifier")]
    [InlineData("Google")]
    [InlineData("Microsoft")]
    [InlineData("OpenIdConnect")]
    public void EnabledProviderWithoutClientId_ShouldFail(string provider)
    {
        ExternalAuthOption options = EnabledProvider(provider);
        SetClientId(options, provider, string.Empty);

        TestValidationResult<ExternalAuthOption> result = _sut.TestValidate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorCode == ExternalOptionOutcome.Failure.ClientIdRequired(provider).Code);
    }

    [Theory(DisplayName = "Enabled providers should require a client secret")]
    [InlineData("Google")]
    [InlineData("Microsoft")]
    [InlineData("OpenIdConnect")]
    public void EnabledProviderWithoutClientSecret_ShouldFail(string provider)
    {
        ExternalAuthOption options = EnabledProvider(provider);
        SetClientSecret(options, provider, string.Empty);

        TestValidationResult<ExternalAuthOption> result = _sut.TestValidate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorCode == ExternalOptionOutcome.Failure.ClientSecretRequired(provider).Code);
    }

    [Theory(DisplayName = "OpenID Connect should require an absolute authority")]
    [InlineData("")]
    [InlineData("/relative")]
    [InlineData("not-a-url")]
    public void OpenIdConnectWithoutAbsoluteAuthority_ShouldFail(string authority)
    {
        ExternalAuthOption options = EnabledProvider("OpenIdConnect");
        options.OpenIdConnect.Authority = authority;

        TestValidationResult<ExternalAuthOption> result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.OpenIdConnect.Authority)
            .WithErrorCode(ExternalOptionOutcome.Failure.OpenIdConnectAuthorityInvalid.Code);
    }

    [Fact(DisplayName = "OpenID Connect should require at least one scope")]
    public void OpenIdConnectWithoutScopes_ShouldFail()
    {
        string[][] invalidScopes =
        [
            [],
            [""],
            ["   "]
        ];

        foreach (string[] scopes in invalidScopes)
        {
            ExternalAuthOption options = EnabledProvider("OpenIdConnect");
            options.OpenIdConnect.Scopes = scopes;

            TestValidationResult<ExternalAuthOption> result = _sut.TestValidate(options);

            result.ShouldHaveValidationErrorFor(x => x.OpenIdConnect.Scopes)
                .WithErrorCode(ExternalOptionOutcome.Failure.OpenIdConnectScopesRequired.Code);
        }
    }

    [Theory(DisplayName = "OpenID Connect should require a callback path")]
    [InlineData("")]
    [InlineData("   ")]
    public void OpenIdConnectWithoutCallbackPath_ShouldFail(string callbackPath)
    {
        ExternalAuthOption options = EnabledProvider("OpenIdConnect");
        options.OpenIdConnect.CallbackPath = callbackPath;

        TestValidationResult<ExternalAuthOption> result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.OpenIdConnect.CallbackPath)
            .WithErrorCode(ExternalOptionOutcome.Failure.OpenIdConnectCallbackPathRequired.Code);
    }

    private static ExternalAuthOption EnabledProvider(string provider)
    {
        ExternalAuthOption options = new();

        switch (provider)
        {
            case "Google":
                options.Google.Enabled = true;
                options.Google.ClientId = "client-id";
                options.Google.ClientSecret = "client-secret";
                break;
            case "Microsoft":
                options.Microsoft.Enabled = true;
                options.Microsoft.ClientId = "client-id";
                options.Microsoft.ClientSecret = "client-secret";
                break;
            case "OpenIdConnect":
                options.OpenIdConnect.Enabled = true;
                options.OpenIdConnect.ClientId = "client-id";
                options.OpenIdConnect.ClientSecret = "client-secret";
                options.OpenIdConnect.Authority = "https://identity.example";
                options.OpenIdConnect.CallbackPath = "/signin-oidc";
                options.OpenIdConnect.Scopes = ["openid", "profile"];
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }

        return options;
    }

    private static void SetClientId(
        ExternalAuthOption options,
        string provider,
        string clientId)
    {
        switch (provider)
        {
            case "Google":
                options.Google.ClientId = clientId;
                break;
            case "Microsoft":
                options.Microsoft.ClientId = clientId;
                break;
            case "OpenIdConnect":
                options.OpenIdConnect.ClientId = clientId;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
    }

    private static void SetClientSecret(
        ExternalAuthOption options,
        string provider,
        string clientSecret)
    {
        switch (provider)
        {
            case "Google":
                options.Google.ClientSecret = clientSecret;
                break;
            case "Microsoft":
                options.Microsoft.ClientSecret = clientSecret;
                break;
            case "OpenIdConnect":
                options.OpenIdConnect.ClientSecret = clientSecret;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
    }
}

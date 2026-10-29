using Application.Common.Integrations.Notifications.Options.Extensions;

using FluentValidation;
using FluentValidation.TestHelper;

namespace Infrastructure.Unit.Tests.Integrations.Notifications.Options.Extensions;
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class ChannelProviderValidationExtensionTests
{
    private sealed class UrlTestValidator : AbstractValidator<string>
    {
        public UrlTestValidator()
        {
            RuleFor(x => x).MustBeValidUrl();
        }
    }
    private sealed class EmailTestValidator : AbstractValidator<string>
    {
        public EmailTestValidator()
        {
            RuleFor(x => x).MustBeValidEmail();
        }
    }
    private sealed class PhoneTestValidator : AbstractValidator<string>
    {
        public PhoneTestValidator()
        {
            RuleFor(x => x).MustBeValidPhone();
        }
    }
    private sealed class EnabledProviderTestValidator : AbstractValidator<Dictionary<string, BaseProviderSetting>>
    {
        public EnabledProviderTestValidator()
        {
            RuleFor(x => x).MustHaveEnabledProvider();
        }
    }
    [Fact(DisplayName = "MustBeValidUrl with valid URL should pass")]
    public void MustBeValidUrl_WithValidUrl_ShouldPass()
    {
        UrlTestValidator validator = new();
        TestValidationResult<string> result = validator.TestValidate("https://example.com");
        result.ShouldNotHaveAnyValidationErrors();
    }
    [Fact(DisplayName = "MustBeValidUrl with invalid URL should fail")]
    public void MustBeValidUrl_WithInvalidUrl_ShouldFail()
    {
        UrlTestValidator validator = new();
        TestValidationResult<string> result = validator.TestValidate("not-a-url");
        result.Errors.Should().NotBeEmpty();
    }
    [Fact(DisplayName = "MustBeValidEmail with valid email should pass")]
    public void MustBeValidEmail_WithValidEmail_ShouldPass()
    {
        EmailTestValidator validator = new();
        TestValidationResult<string> result = validator.TestValidate("user@example.com");
        result.ShouldNotHaveAnyValidationErrors();
    }
    [Fact(DisplayName = "MustBeValidEmail with invalid email should fail")]
    public void MustBeValidEmail_WithInvalidEmail_ShouldFail()
    {
        EmailTestValidator validator = new();
        TestValidationResult<string> result = validator.TestValidate("not-an-email");
        result.Errors.Should().NotBeEmpty();
    }
    [Fact(DisplayName = "MustBeValidPhone with valid phone should pass")]
    public void MustBeValidPhone_WithValidPhone_ShouldPass()
    {
        PhoneTestValidator validator = new();
        TestValidationResult<string> result = validator.TestValidate("+1234567890");
        result.ShouldNotHaveAnyValidationErrors();
    }
    [Fact(DisplayName = "MustBeValidPhone with invalid phone should fail")]
    public void MustBeValidPhone_WithInvalidPhone_ShouldFail()
    {
        PhoneTestValidator validator = new();
        TestValidationResult<string> result = validator.TestValidate("abc");
        result.Errors.Should().NotBeEmpty();
    }
    [Fact(DisplayName = "MustHaveEnabledProvider with enabled provider should pass")]
    public void MustHaveEnabledProvider_WithEnabledProvider_ShouldPass()
    {
        EnabledProviderTestValidator validator = new();
        Dictionary<string, BaseProviderSetting> dict = new()
        {
            ["test"] = new TestProviderSetting { Enabled = true }
        };
        TestValidationResult<Dictionary<string, BaseProviderSetting>> result = validator.TestValidate(dict);
        result.ShouldNotHaveAnyValidationErrors();
    }
    [Fact(DisplayName = "MustHaveEnabledProvider with all disabled should fail")]
    public void MustHaveEnabledProvider_WithAllDisabled_ShouldFail()
    {
        EnabledProviderTestValidator validator = new();
        Dictionary<string, BaseProviderSetting> dict = new()
        {
            ["test"] = new TestProviderSetting { Enabled = false }
        };
        TestValidationResult<Dictionary<string, BaseProviderSetting>> result = validator.TestValidate(dict);
        result.Errors.Should().NotBeEmpty();
    }
    private sealed class TestProviderSetting : BaseProviderSetting
    {
    }
}

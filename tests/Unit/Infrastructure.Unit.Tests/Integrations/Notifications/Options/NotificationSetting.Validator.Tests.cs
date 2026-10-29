using FluentValidation.TestHelper;

using Application.Common.Integrations.Notifications.Options;

namespace Infrastructure.Unit.Tests.Integrations.Notifications.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationSettingValidatorTests
{
    private readonly NotificationSettingValidator _sut = new();

    [Fact(DisplayName = "Should pass when all fields are valid defaults")]
    public void ShouldPass_WhenAllFieldsAreValidDefaults()
    {
        NotificationSetting setting = new();

        TestValidationResult<NotificationSetting> result = _sut.TestValidate(setting);

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Should fail when ApplicationName is empty")]
    public void ShouldFail_WhenApplicationNameIsEmpty()
    {
        NotificationSetting setting = new() { ApplicationName = string.Empty };

        TestValidationResult<NotificationSetting> result = _sut.TestValidate(setting);

        result.ShouldHaveValidationErrorFor(x => x.ApplicationName)
            .WithErrorCode("Notifications.ApplicationName.Required");
    }

    [Fact(DisplayName = "Should fail when SupportEmail is empty")]
    public void ShouldFail_WhenSupportEmailIsEmpty()
    {
        NotificationSetting setting = new() { SupportEmail = string.Empty };

        TestValidationResult<NotificationSetting> result = _sut.TestValidate(setting);

        result.ShouldHaveValidationErrorFor(x => x.SupportEmail)
            .WithErrorCode("Notifications.SupportEmail.Invalid");
    }

    [Fact(DisplayName = "Should fail when SupportEmail is invalid format")]
    public void ShouldFail_WhenSupportEmailIsInvalid()
    {
        NotificationSetting setting = new() { SupportEmail = "not-an-email" };

        TestValidationResult<NotificationSetting> result = _sut.TestValidate(setting);

        result.ShouldHaveValidationErrorFor(x => x.SupportEmail);
    }

    [Fact(DisplayName = "Should fail when SupportPhone is empty")]
    public void ShouldFail_WhenSupportPhoneIsEmpty()
    {
        NotificationSetting setting = new() { SupportPhone = string.Empty };

        TestValidationResult<NotificationSetting> result = _sut.TestValidate(setting);

        result.ShouldHaveValidationErrorFor(x => x.SupportPhone)
            .WithErrorCode("Notifications.SupportPhone.Invalid");
    }

    [Fact(DisplayName = "Should pass when SupportPhone matches pattern")]
    public void ShouldPass_WhenSupportPhoneIsValid()
    {
        NotificationSetting setting = new() { SupportPhone = "+1-555-123-4567" };

        TestValidationResult<NotificationSetting> result = _sut.TestValidate(setting);

        result.ShouldNotHaveValidationErrorFor(x => x.SupportPhone);
    }

    [Fact(DisplayName = "Should fail when ApplicationUrl is empty")]
    public void ShouldFail_WhenApplicationUrlIsEmpty()
    {
        NotificationSetting setting = new() { ApplicationUrl = string.Empty };

        TestValidationResult<NotificationSetting> result = _sut.TestValidate(setting);

        result.ShouldHaveValidationErrorFor(x => x.ApplicationUrl)
            .WithErrorCode("Notifications.ApplicationUrl.Invalid");
    }

    [Fact(DisplayName = "Should fail when ApplicationUrl is not absolute URL")]
    public void ShouldFail_WhenApplicationUrlIsNotAbsolute()
    {
        NotificationSetting setting = new() { ApplicationUrl = "not-a-url" };

        TestValidationResult<NotificationSetting> result = _sut.TestValidate(setting);

        result.ShouldHaveValidationErrorFor(x => x.ApplicationUrl);
    }

    [Fact(DisplayName = "Should fail when CustomerSupportLink is empty")]
    public void ShouldFail_WhenCustomerSupportLinkIsEmpty()
    {
        NotificationSetting setting = new() { CustomerSupportLink = string.Empty };

        TestValidationResult<NotificationSetting> result = _sut.TestValidate(setting);

        result.ShouldHaveValidationErrorFor(x => x.CustomerSupportLink)
            .WithErrorCode("Notifications.CustomerSupportLink.Invalid");
    }

    [Fact(DisplayName = "Should fail when UnsubscribeUrl is empty")]
    public void ShouldFail_WhenUnsubscribeUrlIsEmpty()
    {
        NotificationSetting setting = new() { UnsubscribeUrl = string.Empty };

        TestValidationResult<NotificationSetting> result = _sut.TestValidate(setting);

        result.ShouldHaveValidationErrorFor(x => x.UnsubscribeUrl)
            .WithErrorCode("Notifications.UnsubscribeUrl.Invalid");
    }

    [Fact(DisplayName = "Should fail when SurveyUrl is empty")]
    public void ShouldFail_WhenSurveyUrlIsEmpty()
    {
        NotificationSetting setting = new() { SurveyUrl = string.Empty };

        TestValidationResult<NotificationSetting> result = _sut.TestValidate(setting);

        result.ShouldHaveValidationErrorFor(x => x.SurveyUrl)
            .WithErrorCode("Notifications.SurveyUrl.Invalid");
    }
}

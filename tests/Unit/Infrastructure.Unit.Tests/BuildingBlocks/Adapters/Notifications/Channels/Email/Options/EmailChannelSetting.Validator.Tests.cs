using FluentValidation.TestHelper;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Channels.Email.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class EmailChannelSettingValidatorTests
{
    private readonly EmailChannelSettingValidator _sut = new();

    private static EmailChannelSetting CreateValidSetting() => new()
    {
        FromEmail = "sender@example.com",
        FromName = "Support Team",
};

    [Fact(DisplayName = "Valid EmailChannelSetting should pass all validation rules")]
    public void ValidSetting_ShouldPassValidation()
    {
        EmailChannelSetting setting = CreateValidSetting();
        TestValidationResult<EmailChannelSetting> result = _sut.TestValidate(setting);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "FromEmail empty should have validation error")]
    public void FromEmail_Empty_ShouldHaveError()
    {
        EmailChannelSetting setting = CreateValidSetting();
        setting.FromEmail = string.Empty;
        TestValidationResult<EmailChannelSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.FromEmail);
    }

    [Fact(DisplayName = "FromEmail with invalid format should fail with FromEmailRequired")]
    public void FromEmail_InvalidFormat_ShouldFail()
    {
        EmailChannelSetting setting = CreateValidSetting();
        setting.FromEmail = "not-an-email";
        TestValidationResult<EmailChannelSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.FromEmail)
            .WithErrorCode(EmailChannelSettingOutcome.Failure.FromEmailRequired.Code);
    }

    [Fact(DisplayName = "FromName empty should have validation error")]
    public void FromName_Empty_ShouldHaveError()
    {
        EmailChannelSetting setting = CreateValidSetting();
        setting.FromName = string.Empty;
        TestValidationResult<EmailChannelSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.FromName);
    }


}

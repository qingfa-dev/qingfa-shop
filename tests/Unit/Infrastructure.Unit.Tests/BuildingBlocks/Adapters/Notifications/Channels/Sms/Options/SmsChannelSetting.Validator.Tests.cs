using FluentValidation.TestHelper;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Channels.Sms.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SmsChannelSettingValidatorTests
{
    private readonly SmsChannelSettingValidator _sut = new();

    private static SmsChannelSetting CreateValidSetting() => new()
    {
        DefaultSenderNumber = "+1234567890",
    };

    [Fact(DisplayName = "Valid SmsChannelSetting should pass all validation rules")]
    public void ValidSetting_ShouldPassValidation()
    {
        SmsChannelSetting setting = CreateValidSetting();
        TestValidationResult<SmsChannelSetting> result = _sut.TestValidate(setting);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "DefaultSenderNumber empty should fail with DefaultSenderNumberRequired")]
    public void DefaultSenderNumber_Empty_ShouldFail()
    {
        SmsChannelSetting setting = CreateValidSetting();
        setting.DefaultSenderNumber = string.Empty;
        TestValidationResult<SmsChannelSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.DefaultSenderNumber)
            .WithErrorCode(SmsChannelSettingOutcome.Failure.DefaultSenderNumberRequired.Code);
    }

    [Fact(DisplayName = "DefaultSenderNumber invalid format should fail with DefaultSenderNumberInvalid")]
    public void DefaultSenderNumber_InvalidFormat_ShouldFail()
    {
        SmsChannelSetting setting = CreateValidSetting();
        setting.DefaultSenderNumber = "not-a-phone";
        TestValidationResult<SmsChannelSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.DefaultSenderNumber)
            .WithErrorCode(SmsChannelSettingOutcome.Failure.DefaultSenderNumberInvalid.Code);
    }

    [Fact(DisplayName = "DefaultSenderNumber too long should fail")]
    public void DefaultSenderNumber_TooLong_ShouldFail()
    {
        SmsChannelSetting setting = CreateValidSetting();
        setting.DefaultSenderNumber = "+" + new string('1', 21);
        TestValidationResult<SmsChannelSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.DefaultSenderNumber)
            .WithErrorCode(SmsChannelSettingOutcome.Failure.DefaultSenderNumberInvalid.Code);
    }

    [Fact(DisplayName = "DefaultSenderNumber in valid E.164 format should pass")]
    public void DefaultSenderNumber_ValidE164_ShouldPass()
    {
        SmsChannelSetting setting = CreateValidSetting();
        setting.DefaultSenderNumber = "+441234567890";
        TestValidationResult<SmsChannelSetting> result = _sut.TestValidate(setting);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

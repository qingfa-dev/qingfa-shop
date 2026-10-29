using FluentValidation.TestHelper;

namespace Infrastructure.Unit.Tests.Integrations.Notifications.Channels.Sms.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SinchProviderSettingValidatorTests
{
    private readonly SinchProviderSettingValidator _sut = new();

    private static SinchProviderSetting CreateValidSetting() => new()
    {
        Enabled = true,
        ProjectId = "proj-123",
        KeyId = "key-456",
        KeySecret = "sec-789",
        SenderPhoneNumber = "+1234567890",
    };

    [Fact(DisplayName = "Valid Sinch setting should pass all validation rules")]
    public void ValidSetting_ShouldPassValidation()
    {
        SinchProviderSetting setting = CreateValidSetting();
        TestValidationResult<SinchProviderSetting> result = _sut.TestValidate(setting);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Disabled Sinch setting should pass even with empty values")]
    public void DisabledSetting_ShouldPassValidation()
    {
        SinchProviderSetting setting = CreateValidSetting();
        setting.Enabled = false;
        setting.ProjectId = string.Empty;
        setting.KeyId = string.Empty;
        setting.KeySecret = string.Empty;
        setting.SenderPhoneNumber = string.Empty;
        TestValidationResult<SinchProviderSetting> result = _sut.TestValidate(setting);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "ProjectId empty should fail when enabled")]
    public void ProjectId_Empty_ShouldFail()
    {
        SinchProviderSetting setting = CreateValidSetting();
        setting.ProjectId = string.Empty;
        TestValidationResult<SinchProviderSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.ProjectId)
            .WithErrorCode(SinchProviderSettingResult.Failure.ProjectIdRequired.Code);
    }

    [Fact(DisplayName = "KeyId empty should fail when enabled")]
    public void KeyId_Empty_ShouldFail()
    {
        SinchProviderSetting setting = CreateValidSetting();
        setting.KeyId = string.Empty;
        TestValidationResult<SinchProviderSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.KeyId)
            .WithErrorCode(SinchProviderSettingResult.Failure.KeyIdRequired.Code);
    }

    [Fact(DisplayName = "KeySecret empty should fail when enabled")]
    public void KeySecret_Empty_ShouldFail()
    {
        SinchProviderSetting setting = CreateValidSetting();
        setting.KeySecret = string.Empty;
        TestValidationResult<SinchProviderSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.KeySecret)
            .WithErrorCode(SinchProviderSettingResult.Failure.KeySecretRequired.Code);
    }

    [Fact(DisplayName = "SenderPhoneNumber empty should fail when enabled")]
    public void SenderPhoneNumber_Empty_ShouldFail()
    {
        SinchProviderSetting setting = CreateValidSetting();
        setting.SenderPhoneNumber = string.Empty;
        TestValidationResult<SinchProviderSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.SenderPhoneNumber)
            .WithErrorCode(SinchProviderSettingResult.Failure.SenderPhoneNumberRequired.Code);
    }

    [Fact(DisplayName = "SenderPhoneNumber invalid format should fail")]
    public void SenderPhoneNumber_InvalidFormat_ShouldFail()
    {
        SinchProviderSetting setting = CreateValidSetting();
        setting.SenderPhoneNumber = "not-a-number";
        TestValidationResult<SinchProviderSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.SenderPhoneNumber)
            .WithErrorCode(SinchProviderSettingResult.Failure.SenderPhoneNumberInvalid.Code);
    }

    [Fact(DisplayName = "SenderPhoneNumber too long should fail")]
    public void SenderPhoneNumber_TooLong_ShouldFail()
    {
        SinchProviderSetting setting = CreateValidSetting();
        setting.SenderPhoneNumber = "+" + new string('1', 21);
        TestValidationResult<SinchProviderSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.SenderPhoneNumber);
    }

    [Fact(DisplayName = "All properties with valid values should pass when enabled")]
    public void AllProperties_Valid_ShouldPass()
    {
        SinchProviderSetting setting = CreateValidSetting();
        setting.ProjectId = "abc";
        setting.KeyId = "def";
        setting.KeySecret = "ghi";
        setting.SenderPhoneNumber = "+19876543210";
        TestValidationResult<SinchProviderSetting> result = _sut.TestValidate(setting);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Empty ProjectId but valid others should only report ProjectId error")]
    public void OnlyProjectId_Empty_ShouldReportSingleError()
    {
        SinchProviderSetting setting = CreateValidSetting();
        setting.ProjectId = string.Empty;
        TestValidationResult<SinchProviderSetting> result = _sut.TestValidate(setting);
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
        result.ShouldNotHaveValidationErrorFor(x => x.KeyId);
        result.ShouldNotHaveValidationErrorFor(x => x.KeySecret);
        result.ShouldNotHaveValidationErrorFor(x => x.SenderPhoneNumber);
    }
}

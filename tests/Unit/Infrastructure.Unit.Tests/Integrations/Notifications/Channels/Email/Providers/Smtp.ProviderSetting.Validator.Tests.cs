using FluentValidation.TestHelper;

namespace Infrastructure.Unit.Tests.Integrations.Notifications.Channels.Email.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SmtpProviderSettingValidatorTests
{
    private readonly SmtpProviderSettingValidator _sut = new();

    private static SmtpProviderSetting CreateValidSetting() => new()
    {
        Enabled = true,
        Host = "smtp.example.com",
        Port = 587,
        UseDefaultCredentials = false,
        Username = "user@example.com",
        Password = "s3cret!",
    };

    private static object[] Wrap(string name, Action<SmtpProviderSetting> mutate)
    {
        SmtpProviderSetting setting = CreateValidSetting();
        mutate(setting);
        return [name, setting, true, string.Empty];
    }

    private static object[] Wrap(string name, Action<SmtpProviderSetting> mutate, string expectedErrorCode)
    {
        SmtpProviderSetting setting = CreateValidSetting();
        mutate(setting);
        return [name, setting, false, expectedErrorCode];
    }

    public static IEnumerable<object[]> ValidatorCases()
    {
        yield return Wrap("Valid", s => { });
        yield return Wrap("Disabled", s =>
        {
            s.Enabled = false;
            s.Host = string.Empty;
            s.Port = 0;
            s.Username = null;
            s.Password = null;
        });
        yield return Wrap("Host valid IP", s => { s.Host = "192.168.1.1"; });
        yield return Wrap("Host localhost", s => { s.Host = "localhost"; });
        yield return Wrap("Port at min boundary", s => { s.Port = 2; });
        yield return Wrap("Port at max boundary", s => { s.Port = 65534; });
        yield return Wrap("UseDefaultCredentials true skips credentials", s =>
        {
            s.UseDefaultCredentials = true;
            s.Username = null;
            s.Password = null;
        });
        yield return Wrap("Host empty", s => { s.Host = string.Empty; }, SmtpProviderSettingResult.Failure.SmtpHostRequired.Code);
        yield return Wrap("Host too long", s => { s.Host = new string('a', 257); }, SmtpProviderSettingResult.Failure.SmtpHostTooLong.Code);
        yield return Wrap("Host invalid format", s => { s.Host = "not a valid hostname!!!"; }, SmtpProviderSettingResult.Failure.SmtpHostInvalidFormat.Code);
        yield return Wrap("Host long pathological no timeout", s => { s.Host = new string('a', 1000); }, SmtpProviderSettingResult.Failure.SmtpHostInvalidFormat.Code);
        yield return Wrap("Host long near-miss no timeout", s => { s.Host = new string('a', 500) + "!@#$%"; }, SmtpProviderSettingResult.Failure.SmtpHostInvalidFormat.Code);
        yield return Wrap("Port below minimum", s => { s.Port = 0; }, SmtpProviderSettingResult.Failure.SmtpPortInvalid.Code);
        yield return Wrap("Port above maximum", s => { s.Port = 65536; }, SmtpProviderSettingResult.Failure.SmtpPortOutOfRange.Code);
        yield return Wrap("Username null with UseDefaultCredentials false", s => { s.Username = null; }, SmtpProviderSettingResult.Failure.SmtpCredentialsRequired.Code);
        yield return Wrap("Password null with username provided", s => { s.Password = null; }, SmtpProviderSettingResult.Failure.SmtpPasswordRequired.Code);
    }

    [Theory(DisplayName = "Smtp validator: {0}")]
    [MemberData(nameof(ValidatorCases))]
    public void Validate_ShouldReturnExpected(string caseName, SmtpProviderSetting setting, bool shouldPass, string expectedErrorCode)
    {
        _ = caseName;
        TestValidationResult<SmtpProviderSetting> result = _sut.TestValidate(setting);

        if (shouldPass)
        {
            result.ShouldNotHaveAnyValidationErrors();
        }
        else
        {
            result.Errors.Should().Contain(e => e.ErrorCode == expectedErrorCode);
        }
    }
}

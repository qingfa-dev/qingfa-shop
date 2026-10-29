using FluentValidation.TestHelper;

namespace Infrastructure.Unit.Tests.Integrations.Notifications.Channels.Email.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SendGridProviderSettingValidatorTests
{
    private readonly SendGridProviderSettingValidator _sut = new();

    private static SendGridProviderSetting CreateValidSetting() => new()
    {
        Enabled = true,
        ApiKey = "SG.xxxxxxxxxxxxxxxx.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    };

    public static IEnumerable<object[]> ValidCases()
    {
        yield return ["Valid setting", CreateValidSetting()];

        SendGridProviderSetting disabled = CreateValidSetting();
        disabled.Enabled = false;
        disabled.ApiKey = string.Empty;
        yield return ["Disabled", disabled];

        SendGridProviderSetting minBoundary = CreateValidSetting();
        minBoundary.ApiKey = "SG." + new string('x', 6) + "." + new string('y', 1);
        yield return ["ApiKey min length boundary", minBoundary];
    }

    [Theory(DisplayName = "Valid case: {0}")]
    [MemberData(nameof(ValidCases))]
    public void ValidSetting_ShouldPass(string caseName, SendGridProviderSetting setting)
    {
        _ = caseName;
        _sut.TestValidate(setting).ShouldNotHaveAnyValidationErrors();
    }

    public static IEnumerable<object[]> InvalidCases()
    {
        yield return ["ApiKey empty", "", SendGridProviderSettingResult.Failure.ApiKeyRequired.Code];

        yield return ["ApiKey too short", "SG.ab", SendGridProviderSettingResult.Failure.ApiKeyTooShort.Code];

        yield return ["ApiKey too long", "SG." + new string('x', 300), SendGridProviderSettingResult.Failure.ApiKeyTooLong.Code
        ];

        yield return ["ApiKey missing SG prefix", "not-a-valid-key", SendGridProviderSettingResult.Failure.ApiKeyInvalidFormat.Code
        ];

        yield return ["ApiKey with whitespace", "SG.xxx xxx.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", SendGridProviderSettingResult.Failure.ApiKeyContainsWhitespace.Code
        ];
    }

    [Theory(DisplayName = "Invalid case: {0}")]
    [MemberData(nameof(InvalidCases))]
    public void InvalidSetting_ShouldFail(string caseName, string apiKey, string expectedErrorCode)
    {
        _ = caseName;
        SendGridProviderSetting setting = CreateValidSetting();
        setting.ApiKey = apiKey;
        TestValidationResult<SendGridProviderSetting> result = _sut.TestValidate(setting);
        result.Errors.Should().Contain(e => e.ErrorCode == expectedErrorCode);
    }

    [Fact(DisplayName = "ApiKey with multiple violations should report all relevant errors")]
    public void ApiKey_MultipleViolations_ShouldReportAll()
    {
        SendGridProviderSetting setting = CreateValidSetting();
        setting.ApiKey = "short";
        TestValidationResult<SendGridProviderSetting> result = _sut.TestValidate(setting);
        result.Errors.Should().Contain(e => e.ErrorCode == SendGridProviderSettingResult.Failure.ApiKeyInvalidFormat.Code);
        result.Errors.Should().Contain(e => e.ErrorCode == SendGridProviderSettingResult.Failure.ApiKeyTooShort.Code);
    }
}

using FluentValidation.TestHelper;

using Application.Common.Integrations.Storages.Providers.Options;

namespace Infrastructure.Unit.Tests.Integrations.Storages.Providers.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Storage")]
public sealed class LocalStorageProviderSettingValidatorTests
{
    private readonly LocalStorageProviderSettingValidator _sut = new();

    public static TheoryData<
        Action<LocalStorageProviderSetting>,
        string> InvalidCases =>
        new()
        {
            {
                x => x.LocalPath = string.Empty,
                LocalStorageProviderResult.Failure.LocalPathRequired.Code
            },
            {
                x => x.LocalPath = "invalid\0path",
                LocalStorageProviderResult.Failure.LocalPathInvalid.Code
            },
            {
                x => x.BufferSize = 0,
                LocalStorageProviderResult.Failure.BufferSizeInvalid.Code
            }
        };

    [Fact(DisplayName = "Validate should pass for default options")]
    public void Validate_WithDefaultOptions_ShouldPass()
    {
        LocalStorageProviderSetting options = new();

        TestValidationResult<LocalStorageProviderSetting> result = _sut.TestValidate(options);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Validate_WithInvalidValue_ShouldFail(
        Action<LocalStorageProviderSetting> setup,
        string expectedErrorCode)
    {
        LocalStorageProviderSetting options = new();
        setup(options);

        TestValidationResult<LocalStorageProviderSetting> result = _sut.TestValidate(options);

        result.Errors.Should().Contain(e => e.ErrorCode == expectedErrorCode);
    }
}

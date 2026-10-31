using FluentValidation.TestHelper;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Storage.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Storage")]
public sealed class StorageSettingValidatorTests
{
    private readonly StorageSettingValidator _sut = new();

    public static TheoryData<
        Action<StorageSetting>,
        string> InvalidCases =>
        new()
        {
            {
                x => x.DefaultProvider = string.Empty,
                StorageSettingOutcome.Failure.DefaultProviderRequired.Code
            },
            {
                x => x.Security = null!,
                StorageSettingOutcome.Failure.SecurityRequired.Code
            },
            {
                x => x.BaseUrl = "not-a-valid-url",
                StorageSettingOutcome.Failure.BaseUrlInvalid.Code
            }
        };

    [Fact(DisplayName = "Validate should pass for default options")]
    public void Validate_WithDefaultOptions_ShouldPass()
    {
        StorageSetting options = new();

        TestValidationResult<StorageSetting> result = _sut.TestValidate(options);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Validate_WithInvalidValue_ShouldFail(
        Action<StorageSetting> setup,
        string expectedErrorCode)
    {
        StorageSetting options = new();
        setup(options);

        TestValidationResult<StorageSetting> result = _sut.TestValidate(options);

        result.Errors.Should().Contain(e => e.ErrorCode == expectedErrorCode);
    }
}

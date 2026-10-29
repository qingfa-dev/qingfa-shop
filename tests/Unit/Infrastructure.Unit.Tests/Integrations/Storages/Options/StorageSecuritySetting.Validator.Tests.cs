using FluentValidation.TestHelper;

namespace Infrastructure.Unit.Tests.Integrations.Storages.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Storage")]
public sealed class StorageSecuritySettingValidatorTests
{
    private readonly StorageSecuritySettingValidator _sut = new();

    public static TheoryData<
        Action<StorageSecuritySetting>,
        string> InvalidCases =>
        new()
        {
            {
                x => x.MaxFileSizeBytes = 0L,
                StorageSecuritySettingResult.Failure.MaxFileSizeBytesInvalid.Code
            },
            {
                x => x.AllowedExtensions = null!,
                StorageSecuritySettingResult.Failure.AllowedExtensionsRequired.Code
            },
            {
                x => x.BlockedExtensions = null!,
                StorageSecuritySettingResult.Failure.BlockedExtensionsRequired.Code
            },
            {
                x => x.EncryptionKey = "too-short",
                StorageSecuritySettingResult.Failure.EncryptionKeyInvalid.Code
            }
        };

    [Fact(DisplayName = "Validate should pass for default options")]
    public void Validate_WithDefaultOptions_ShouldPass()
    {
        StorageSecuritySetting options = new();

        TestValidationResult<StorageSecuritySetting> result = _sut.TestValidate(options);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Validate_WithInvalidValue_ShouldFail(
        Action<StorageSecuritySetting> setup,
        string expectedErrorCode)
    {
        StorageSecuritySetting options = new();
        setup(options);

        TestValidationResult<StorageSecuritySetting> result = _sut.TestValidate(options);

        result.Errors.Should().Contain(e => e.ErrorCode == expectedErrorCode);
    }
}

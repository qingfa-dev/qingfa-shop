using FluentValidation.TestHelper;

namespace Infrastructure.Unit.Tests.Integrations.Storages.Providers.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Storage")]
public sealed class S3StorageProviderSettingValidatorTests
{
    private readonly S3StorageProviderSettingValidator _sut = new();

    public static TheoryData<
        Action<S3StorageProviderSetting>,
        string> InvalidCases =>
        new()
        {
            {
                x => x.ServiceUrl = "not-a-url",
                S3StorageProviderResult.Failure.ServiceUrlInvalid.Code
            },
            {
                x => x.AccessKey = string.Empty,
                S3StorageProviderResult.Failure.AccessKeyRequired.Code
            },
            {
                x => x.SecretKey = string.Empty,
                S3StorageProviderResult.Failure.SecretKeyRequired.Code
            },
            {
                x => x.BucketName = string.Empty,
                S3StorageProviderResult.Failure.BucketNameRequired.Code
            },
            {
                x => x.BucketName = "ab",
                S3StorageProviderResult.Failure.BucketNameInvalid.Code
            },
            {
                x => x.BucketName = "INVALID",
                S3StorageProviderResult.Failure.BucketNameInvalid.Code
            },
            {
                x => x.Region = string.Empty,
                S3StorageProviderResult.Failure.RegionRequired.Code
            },
            {
                x => x.BufferSize = 0,
                S3StorageProviderResult.Failure.BufferSizeInvalid.Code
            }
        };

    [Fact(DisplayName = "Validate should pass for default options")]
    public void Validate_WithDefaultOptions_ShouldPass()
    {
        S3StorageProviderSetting options = new()
        {
            AccessKey = "AKIAIOSFODNN7EXAMPLE",
            SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        };

        TestValidationResult<S3StorageProviderSetting> result = _sut.TestValidate(options);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Validate_WithInvalidValue_ShouldFail(
        Action<S3StorageProviderSetting> setup,
        string expectedErrorCode)
    {
        S3StorageProviderSetting options = new();
        setup(options);

        TestValidationResult<S3StorageProviderSetting> result = _sut.TestValidate(options);

        result.Errors.Should().Contain(e => e.ErrorCode == expectedErrorCode);
    }
}

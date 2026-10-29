using FluentValidation.TestHelper;

namespace Infrastructure.Unit.Tests.Integrations.Storages.Providers.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Storage")]
public sealed class AzureStorageProviderSettingValidatorTests
{
    private readonly AzureStorageProviderSettingValidator _sut = new();

    public static TheoryData<
        Action<AzureStorageProviderSetting>,
        string> InvalidCases =>
        new()
        {
            {
                x => x.ConnectionString = string.Empty,
                AzureStorageProviderResult.Failure.ConnectionStringRequired.Code
            },
            {
                x => x.ContainerName = string.Empty,
                AzureStorageProviderResult.Failure.ContainerNameRequired.Code
            },
            {
                x => x.ContainerName = "INVALID",
                AzureStorageProviderResult.Failure.ContainerNameInvalid.Code
            },
            {
                x => x.BufferSize = 0,
                AzureStorageProviderResult.Failure.BufferSizeInvalid.Code
            }
        };

    [Fact(DisplayName = "Validate should pass for valid options")]
    public void Validate_WithValidOptions_ShouldPass()
    {
        AzureStorageProviderSetting options = new()
        {
            ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net"
        };

        TestValidationResult<AzureStorageProviderSetting> result = _sut.TestValidate(options);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Validate_WithInvalidValue_ShouldFail(
        Action<AzureStorageProviderSetting> setup,
        string expectedErrorCode)
    {
        AzureStorageProviderSetting options = new();
        setup(options);

        TestValidationResult<AzureStorageProviderSetting> result = _sut.TestValidate(options);

        result.Errors.Should().Contain(e => e.ErrorCode == expectedErrorCode);
    }
}

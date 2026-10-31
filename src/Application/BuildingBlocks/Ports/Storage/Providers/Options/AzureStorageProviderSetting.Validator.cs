using FluentValidation;

namespace Application.BuildingBlocks.Ports.Storage.Providers.Options;

public sealed class AzureStorageProviderSettingValidator : AbstractValidator<AzureStorageProviderSetting>
{
    public AzureStorageProviderSettingValidator()
    {
        When(x => x.IsEnabled, (Action)(() =>
        {
            RuleFor(x => x.ConnectionString)
                .NotEmpty()
                .WithErrorCode(AzureStorageProviderOutcome.Failure.ConnectionStringRequired.Code)
                .WithMessage((string)AzureStorageProviderOutcome.Failure.ConnectionStringRequired.Message);

            RuleFor(x => x.ContainerName)
                .NotEmpty()
                .WithErrorCode(AzureStorageProviderOutcome.Failure.ContainerNameRequired.Code)
                .WithMessage((string)AzureStorageProviderOutcome.Failure.ContainerNameRequired.Message)
                .Must(BeValidContainerName)
                .WithErrorCode(AzureStorageProviderOutcome.Failure.ContainerNameInvalid.Code)
                .WithMessage((string)AzureStorageProviderOutcome.Failure.ContainerNameInvalid.Message);

            RuleFor(x => x.BufferSize)
                .GreaterThanOrEqualTo(AzureStorageProviderConstant.Constraints.BufferSizeMin)
                .WithErrorCode(AzureStorageProviderOutcome.Failure.BufferSizeInvalid.Code)
                .WithMessage((string)AzureStorageProviderOutcome.Failure.BufferSizeInvalid.Message);
        }));
    }

    private static bool BeValidContainerName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return true;

        if (name.Length < 3 || name.Length > AzureStorageProviderConstant.Constraints.ContainerNameMaxLength)
            return false;

        return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-z0-9]([a-z0-9]|-(?!-))*[a-z0-9]$");
    }
}

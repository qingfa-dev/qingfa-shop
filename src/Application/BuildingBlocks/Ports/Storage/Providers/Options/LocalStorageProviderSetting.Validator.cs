using FluentValidation;

namespace Application.BuildingBlocks.Ports.Storage.Providers.Options;

public sealed class LocalStorageProviderSettingValidator : AbstractValidator<LocalStorageProviderSetting>
{
    public LocalStorageProviderSettingValidator()
    {
        When(x => x.IsEnabled, (Action)(() =>
        {
            RuleFor(x => x.LocalPath)
                .NotEmpty()
                .WithErrorCode(LocalStorageProviderOutcome.Failure.LocalPathRequired.Code)
                .WithMessage((string)LocalStorageProviderOutcome.Failure.LocalPathRequired.Message)
                .Must(path => !string.IsNullOrWhiteSpace(path) && path.IndexOfAny(Path.GetInvalidPathChars()) == -1)
                .WithErrorCode(LocalStorageProviderOutcome.Failure.LocalPathInvalid.Code)
                .WithMessage((string)LocalStorageProviderOutcome.Failure.LocalPathInvalid.Message);

            RuleFor(x => x.BufferSize)
                .GreaterThanOrEqualTo(LocalStorageProviderConstant.Constraints.BufferSizeMin)
                .WithErrorCode(LocalStorageProviderOutcome.Failure.BufferSizeInvalid.Code)
                .WithMessage((string)LocalStorageProviderOutcome.Failure.BufferSizeInvalid.Message);
        }));
    }
}

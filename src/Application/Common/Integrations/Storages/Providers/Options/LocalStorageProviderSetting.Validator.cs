using FluentValidation;

namespace Application.Common.Integrations.Storages.Providers.Options;

public sealed class LocalStorageProviderSettingValidator : AbstractValidator<LocalStorageProviderSetting>
{
    public LocalStorageProviderSettingValidator()
    {
        When(x => x.IsEnabled, (Action)(() =>
        {
            RuleFor(x => x.LocalPath)
                .NotEmpty()
                .WithErrorCode(LocalStorageProviderResult.Failure.LocalPathRequired.Code)
                .WithMessage((string)LocalStorageProviderResult.Failure.LocalPathRequired.Message)
                .Must(path => !string.IsNullOrWhiteSpace(path) && path.IndexOfAny(Path.GetInvalidPathChars()) == -1)
                .WithErrorCode(LocalStorageProviderResult.Failure.LocalPathInvalid.Code)
                .WithMessage((string)LocalStorageProviderResult.Failure.LocalPathInvalid.Message);

            RuleFor(x => x.BufferSize)
                .GreaterThanOrEqualTo(LocalStorageProviderConstant.Constraints.BufferSizeMin)
                .WithErrorCode(LocalStorageProviderResult.Failure.BufferSizeInvalid.Code)
                .WithMessage((string)LocalStorageProviderResult.Failure.BufferSizeInvalid.Message);
        }));
    }
}

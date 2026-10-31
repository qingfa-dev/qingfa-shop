using FluentValidation;

using Application.BuildingBlocks.Ports.Storage.Security.Options;

namespace Application.BuildingBlocks.Ports.Storage.Options;

public sealed class StorageSettingValidator : AbstractValidator<StorageSetting>
{
    public StorageSettingValidator()
    {
        RuleFor(x => x.DefaultProvider)
            .NotEmpty()
            .WithErrorCode(StorageSettingOutcome.Failure.DefaultProviderRequired.Code)
            .WithMessage(StorageSettingOutcome.Failure.DefaultProviderRequired.Message);

        RuleFor(x => x.BaseUrl)
            .Must(BeValidUrlOrEmpty)
            .WithErrorCode(StorageSettingOutcome.Failure.BaseUrlInvalid.Code)
            .WithMessage(StorageSettingOutcome.Failure.BaseUrlInvalid.Message);

        RuleFor(x => x.Security)
            .NotNull()
            .WithErrorCode(StorageSettingOutcome.Failure.SecurityRequired.Code)
            .WithMessage(StorageSettingOutcome.Failure.SecurityRequired.Message);
        RuleFor(x => x.Security!).SetValidator(new StorageSecuritySettingValidator());

    }

    private static bool BeValidUrlOrEmpty(string url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out Uri? result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
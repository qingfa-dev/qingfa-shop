using FluentValidation;

using Application.Common.Integrations.Storages.Security.Options;

namespace Application.Common.Integrations.Storages.Options;

public sealed class StorageSettingValidator : AbstractValidator<StorageSetting>
{
    public StorageSettingValidator()
    {
        RuleFor(x => x.DefaultProvider)
            .NotEmpty()
            .WithErrorCode(StorageSettingResult.Failure.DefaultProviderRequired.Code)
            .WithMessage(StorageSettingResult.Failure.DefaultProviderRequired.Message);

        RuleFor(x => x.BaseUrl)
            .Must(BeValidUrlOrEmpty)
            .WithErrorCode(StorageSettingResult.Failure.BaseUrlInvalid.Code)
            .WithMessage(StorageSettingResult.Failure.BaseUrlInvalid.Message);

        RuleFor(x => x.Security)
            .NotNull()
            .WithErrorCode(StorageSettingResult.Failure.SecurityRequired.Code)
            .WithMessage(StorageSettingResult.Failure.SecurityRequired.Message);
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
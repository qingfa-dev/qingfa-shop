using FluentValidation;

namespace Application.Common.Integrations.Storages.Security.Options;

public sealed class StorageSecuritySettingValidator : AbstractValidator<StorageSecuritySetting>
{
    public StorageSecuritySettingValidator()
    {
        RuleFor(x => x.MaxFileSizeBytes)
            .GreaterThanOrEqualTo(StorageSecuritySettingConstant.Constraints.MaxFileSizeBytesMin)
            .WithErrorCode(StorageSecuritySettingResult.Failure.MaxFileSizeBytesInvalid.Code)
            .WithMessage(StorageSecuritySettingResult.Failure.MaxFileSizeBytesInvalid.Message);

        RuleFor(x => x.AllowedExtensions)
            .NotNull()
            .WithErrorCode(StorageSecuritySettingResult.Failure.AllowedExtensionsRequired.Code)
            .WithMessage(StorageSecuritySettingResult.Failure.AllowedExtensionsRequired.Message);

        RuleFor(x => x.BlockedExtensions)
            .NotNull()
            .WithErrorCode(StorageSecuritySettingResult.Failure.BlockedExtensionsRequired.Code)
            .WithMessage(StorageSecuritySettingResult.Failure.BlockedExtensionsRequired.Message);

        RuleFor(x => x.EncryptionKey)
            .Must(BeValidEncryptionKeyOrEmpty)
            .WithErrorCode(StorageSecuritySettingResult.Failure.EncryptionKeyInvalid.Code)
            .WithMessage(StorageSecuritySettingResult.Failure.EncryptionKeyInvalid.Message);
    }

    private static bool BeValidEncryptionKeyOrEmpty(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return true;

        return StorageSecuritySettingConstant.Constraints.ValidEncryptionKeyLengths.Contains(key.Length);
    }
}

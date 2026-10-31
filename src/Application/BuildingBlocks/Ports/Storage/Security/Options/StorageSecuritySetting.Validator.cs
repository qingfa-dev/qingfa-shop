using FluentValidation;

namespace Application.BuildingBlocks.Ports.Storage.Security.Options;

public sealed class StorageSecuritySettingValidator : AbstractValidator<StorageSecuritySetting>
{
    public StorageSecuritySettingValidator()
    {
        RuleFor(x => x.MaxFileSizeBytes)
            .GreaterThanOrEqualTo(StorageSecuritySettingConstant.Constraints.MaxFileSizeBytesMin)
            .WithErrorCode(StorageSecuritySettingOutcome.Failure.MaxFileSizeBytesInvalid.Code)
            .WithMessage(StorageSecuritySettingOutcome.Failure.MaxFileSizeBytesInvalid.Message);

        RuleFor(x => x.AllowedExtensions)
            .NotNull()
            .WithErrorCode(StorageSecuritySettingOutcome.Failure.AllowedExtensionsRequired.Code)
            .WithMessage(StorageSecuritySettingOutcome.Failure.AllowedExtensionsRequired.Message);

        RuleFor(x => x.BlockedExtensions)
            .NotNull()
            .WithErrorCode(StorageSecuritySettingOutcome.Failure.BlockedExtensionsRequired.Code)
            .WithMessage(StorageSecuritySettingOutcome.Failure.BlockedExtensionsRequired.Message);

        RuleFor(x => x.EncryptionKey)
            .Must(BeValidEncryptionKeyOrEmpty)
            .WithErrorCode(StorageSecuritySettingOutcome.Failure.EncryptionKeyInvalid.Code)
            .WithMessage(StorageSecuritySettingOutcome.Failure.EncryptionKeyInvalid.Message);
    }

    private static bool BeValidEncryptionKeyOrEmpty(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return true;

        return StorageSecuritySettingConstant.Constraints.ValidEncryptionKeyLengths.Contains(key.Length);
    }
}

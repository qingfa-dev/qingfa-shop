using System.Text.RegularExpressions;

using FluentValidation;

namespace Application.BuildingBlocks.Ports.Storage.Providers.Options;

public sealed class S3StorageProviderSettingValidator : AbstractValidator<S3StorageProviderSetting>
{
    private static readonly Regex BucketNameRegex = new(
        @"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$",
        RegexOptions.Compiled);

    public S3StorageProviderSettingValidator()
    {
        When(x => x.IsEnabled, (Action)(() =>
        {
            When(x => !string.IsNullOrEmpty(x.ServiceUrl), (Action)(() =>
            {
                RuleFor(x => x.ServiceUrl)
                    .MaximumLength(S3StorageProviderConstant.Constraints.ServiceUrlMaxLength)
                    .WithErrorCode(S3StorageProviderOutcome.Failure.ServiceUrlInvalid.Code)
                    .WithMessage((string)S3StorageProviderOutcome.Failure.ServiceUrlInvalid.Message)
                    .Must(BeValidUri)
                    .WithErrorCode(S3StorageProviderOutcome.Failure.ServiceUrlInvalid.Code)
                    .WithMessage((string)S3StorageProviderOutcome.Failure.ServiceUrlInvalid.Message);
            }));

            RuleFor(x => x.AccessKey)
                .NotEmpty()
                .WithErrorCode(S3StorageProviderOutcome.Failure.AccessKeyRequired.Code)
                .WithMessage((string)S3StorageProviderOutcome.Failure.AccessKeyRequired.Message)
                .MaximumLength(S3StorageProviderConstant.Constraints.AccessKeyMaxLength)
                .WithErrorCode(S3StorageProviderOutcome.Failure.AccessKeyRequired.Code)
                .WithMessage((string)S3StorageProviderOutcome.Failure.AccessKeyRequired.Message);

            RuleFor(x => x.SecretKey)
                .NotEmpty()
                .WithErrorCode(S3StorageProviderOutcome.Failure.SecretKeyRequired.Code)
                .WithMessage((string)S3StorageProviderOutcome.Failure.SecretKeyRequired.Message)
                .MaximumLength(S3StorageProviderConstant.Constraints.SecretKeyMaxLength)
                .WithErrorCode(S3StorageProviderOutcome.Failure.SecretKeyRequired.Code)
                .WithMessage((string)S3StorageProviderOutcome.Failure.SecretKeyRequired.Message);

            RuleFor(x => x.BucketName)
                .NotEmpty()
                .WithErrorCode(S3StorageProviderOutcome.Failure.BucketNameRequired.Code)
                .WithMessage((string)S3StorageProviderOutcome.Failure.BucketNameRequired.Message)
                .Length(S3StorageProviderConstant.Constraints.BucketNameMinLength, S3StorageProviderConstant.Constraints.BucketNameMaxLength)
                .WithErrorCode(S3StorageProviderOutcome.Failure.BucketNameInvalid.Code)
                .WithMessage((string)S3StorageProviderOutcome.Failure.BucketNameInvalid.Message)
                .Must(name => BucketNameRegex.IsMatch(name))
                .WithErrorCode(S3StorageProviderOutcome.Failure.BucketNameInvalid.Code)
                .WithMessage((string)S3StorageProviderOutcome.Failure.BucketNameInvalid.Message);

            RuleFor(x => x.Region)
                .NotEmpty()
                .WithErrorCode(S3StorageProviderOutcome.Failure.RegionRequired.Code)
                .WithMessage((string)S3StorageProviderOutcome.Failure.RegionRequired.Message)
                .MaximumLength(S3StorageProviderConstant.Constraints.RegionMaxLength)
                .WithErrorCode(S3StorageProviderOutcome.Failure.RegionInvalid.Code)
                .WithMessage((string)S3StorageProviderOutcome.Failure.RegionInvalid.Message);

            RuleFor(x => x.BufferSize)
                .GreaterThanOrEqualTo(S3StorageProviderConstant.Constraints.BufferSizeMin)
                .WithErrorCode(S3StorageProviderOutcome.Failure.BufferSizeInvalid.Code)
                .WithMessage((string)S3StorageProviderOutcome.Failure.BufferSizeInvalid.Message);
        }));
    }

    private static bool BeValidUri(string? uri)
    {
        return Uri.TryCreate(uri, UriKind.Absolute, out _);
    }
}

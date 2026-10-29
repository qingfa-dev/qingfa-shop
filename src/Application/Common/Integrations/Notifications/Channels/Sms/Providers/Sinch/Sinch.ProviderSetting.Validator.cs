using FluentValidation;

namespace Application.Common.Integrations.Notifications.Channels.Sms.Providers.Sinch;

/// <summary>Validates Sinch SMS provider configuration settings.</summary>
public sealed class SinchProviderSettingValidator : AbstractValidator<SinchProviderSetting>
{
    public SinchProviderSettingValidator()
    {
        // Validate: Ensure ProjectId is provided when provider is enabled
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SinchProviderSettingResult.Failure.ProjectIdRequired.Code)
            .WithMessage(SinchProviderSettingResult.Failure.ProjectIdRequired.Message);

        // Validate: Enforce ProjectId length constraints
        RuleFor(x => x.ProjectId)
            .MinimumLength(SinchProviderSettingConstant.Constraints.ProjectIdMinLength)
            .MaximumLength(SinchProviderSettingConstant.Constraints.ProjectIdMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.ProjectId))
            .WithErrorCode(SinchProviderSettingResult.Failure.ProjectIdInvalidLength.Code)
            .WithMessage(SinchProviderSettingResult.Failure.ProjectIdInvalidLength.Message);

        // Validate: Ensure KeyId is provided when provider is enabled
        RuleFor(x => x.KeyId)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SinchProviderSettingResult.Failure.KeyIdRequired.Code)
            .WithMessage(SinchProviderSettingResult.Failure.KeyIdRequired.Message);

        // Validate: Enforce KeyId length constraints
        RuleFor(x => x.KeyId)
            .MinimumLength(SinchProviderSettingConstant.Constraints.KeyIdMinLength)
            .MaximumLength(SinchProviderSettingConstant.Constraints.KeyIdMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.KeyId))
            .WithErrorCode(SinchProviderSettingResult.Failure.KeyIdInvalidLength.Code)
            .WithMessage(SinchProviderSettingResult.Failure.KeyIdInvalidLength.Message);

        // Validate: Ensure KeySecret is provided when provider is enabled
        RuleFor(x => x.KeySecret)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SinchProviderSettingResult.Failure.KeySecretRequired.Code)
            .WithMessage(SinchProviderSettingResult.Failure.KeySecretRequired.Message);

        // Validate: Enforce KeySecret length constraints
        RuleFor(x => x.KeySecret)
            .MinimumLength(SinchProviderSettingConstant.Constraints.KeySecretMinLength)
            .MaximumLength(SinchProviderSettingConstant.Constraints.KeySecretMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.KeySecret))
            .WithErrorCode(SinchProviderSettingResult.Failure.KeySecretInvalidLength.Code)
            .WithMessage(SinchProviderSettingResult.Failure.KeySecretInvalidLength.Message);

        // Validate: Ensure SenderPhoneNumber is provided when enabled
        RuleFor(x => x.SenderPhoneNumber)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SinchProviderSettingResult.Failure.SenderPhoneNumberRequired.Code)
            .WithMessage(SinchProviderSettingResult.Failure.SenderPhoneNumberRequired.Message);

        // Validate: Enforce SenderPhoneNumber maximum length
        RuleFor(x => x.SenderPhoneNumber)
            .MaximumLength(SinchProviderSettingConstant.Constraints.SenderPhoneNumberMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.SenderPhoneNumber))
            .WithErrorCode(SinchProviderSettingResult.Failure.SenderPhoneNumberInvalid.Code)
            .WithMessage(SinchProviderSettingResult.Failure.SenderPhoneNumberInvalid.Message);

        // Validate: Verify SenderPhoneNumber matches E.164 format
        RuleFor(x => x.SenderPhoneNumber)
            .Matches(SinchProviderSettingConstant.Patterns.SenderPhoneNumber)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.SenderPhoneNumber))
            .WithErrorCode(SinchProviderSettingResult.Failure.SenderPhoneNumberInvalid.Code)
            .WithMessage(SinchProviderSettingResult.Failure.SenderPhoneNumberInvalid.Message);
    }
}

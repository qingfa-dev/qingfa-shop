using FluentValidation;

namespace Application.BuildingBlocks.Ports.Notifications.Channels.Sms.Providers.Sinch;

/// <summary>Validates Sinch SMS provider configuration settings.</summary>
public sealed class SinchProviderSettingValidator : AbstractValidator<SinchProviderSetting>
{
    public SinchProviderSettingValidator()
    {
        // Validate: Ensure ProjectId is provided when provider is enabled
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SinchProviderSettingOutcome.Failure.ProjectIdRequired.Code)
            .WithMessage(SinchProviderSettingOutcome.Failure.ProjectIdRequired.Message);

        // Validate: Enforce ProjectId length constraints
        RuleFor(x => x.ProjectId)
            .MinimumLength(SinchProviderSettingConstant.Constraints.ProjectIdMinLength)
            .MaximumLength(SinchProviderSettingConstant.Constraints.ProjectIdMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.ProjectId))
            .WithErrorCode(SinchProviderSettingOutcome.Failure.ProjectIdInvalidLength.Code)
            .WithMessage(SinchProviderSettingOutcome.Failure.ProjectIdInvalidLength.Message);

        // Validate: Ensure KeyId is provided when provider is enabled
        RuleFor(x => x.KeyId)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SinchProviderSettingOutcome.Failure.KeyIdRequired.Code)
            .WithMessage(SinchProviderSettingOutcome.Failure.KeyIdRequired.Message);

        // Validate: Enforce KeyId length constraints
        RuleFor(x => x.KeyId)
            .MinimumLength(SinchProviderSettingConstant.Constraints.KeyIdMinLength)
            .MaximumLength(SinchProviderSettingConstant.Constraints.KeyIdMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.KeyId))
            .WithErrorCode(SinchProviderSettingOutcome.Failure.KeyIdInvalidLength.Code)
            .WithMessage(SinchProviderSettingOutcome.Failure.KeyIdInvalidLength.Message);

        // Validate: Ensure KeySecret is provided when provider is enabled
        RuleFor(x => x.KeySecret)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SinchProviderSettingOutcome.Failure.KeySecretRequired.Code)
            .WithMessage(SinchProviderSettingOutcome.Failure.KeySecretRequired.Message);

        // Validate: Enforce KeySecret length constraints
        RuleFor(x => x.KeySecret)
            .MinimumLength(SinchProviderSettingConstant.Constraints.KeySecretMinLength)
            .MaximumLength(SinchProviderSettingConstant.Constraints.KeySecretMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.KeySecret))
            .WithErrorCode(SinchProviderSettingOutcome.Failure.KeySecretInvalidLength.Code)
            .WithMessage(SinchProviderSettingOutcome.Failure.KeySecretInvalidLength.Message);

        // Validate: Ensure SenderPhoneNumber is provided when enabled
        RuleFor(x => x.SenderPhoneNumber)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SinchProviderSettingOutcome.Failure.SenderPhoneNumberRequired.Code)
            .WithMessage(SinchProviderSettingOutcome.Failure.SenderPhoneNumberRequired.Message);

        // Validate: Enforce SenderPhoneNumber maximum length
        RuleFor(x => x.SenderPhoneNumber)
            .MaximumLength(SinchProviderSettingConstant.Constraints.SenderPhoneNumberMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.SenderPhoneNumber))
            .WithErrorCode(SinchProviderSettingOutcome.Failure.SenderPhoneNumberInvalid.Code)
            .WithMessage(SinchProviderSettingOutcome.Failure.SenderPhoneNumberInvalid.Message);

        // Validate: Verify SenderPhoneNumber matches E.164 format
        RuleFor(x => x.SenderPhoneNumber)
            .Matches(SinchProviderSettingConstant.Patterns.SenderPhoneNumber)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.SenderPhoneNumber))
            .WithErrorCode(SinchProviderSettingOutcome.Failure.SenderPhoneNumberInvalid.Code)
            .WithMessage(SinchProviderSettingOutcome.Failure.SenderPhoneNumberInvalid.Message);
    }
}

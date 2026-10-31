using FluentValidation;

namespace Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Providers.SendGrid;

/// <summary>Validates SendGrid provider configuration settings.</summary>
public sealed class SendGridProviderSettingValidator : AbstractValidator<SendGridProviderSetting>
{
    public SendGridProviderSettingValidator()
    {
        // Validate: Ensure API key is provided when provider is enabled
        RuleFor(x => x.ApiKey)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SendGridProviderSettingOutcome.Failure.ApiKeyRequired.Code)
            .WithMessage(SendGridProviderSettingOutcome.Failure.ApiKeyRequired.Message);

        // Validate: Enforce minimum API key length
        RuleFor(x => x.ApiKey)
            .MinimumLength(SendGridProviderSettingConstant.Constraints.ApiKeyMinLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.ApiKey))
            .WithErrorCode(SendGridProviderSettingOutcome.Failure.ApiKeyTooShort.Code)
            .WithMessage(SendGridProviderSettingOutcome.Failure.ApiKeyTooShort.Message);

        // Validate: Enforce maximum API key length
        RuleFor(x => x.ApiKey)
            .MaximumLength(SendGridProviderSettingConstant.Constraints.ApiKeyMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.ApiKey))
            .WithErrorCode(SendGridProviderSettingOutcome.Failure.ApiKeyTooLong.Code)
            .WithMessage(SendGridProviderSettingOutcome.Failure.ApiKeyTooLong.Message);

        // Validate: Verify API key matches expected format (SG.xxx.xxx)
        RuleFor(x => x.ApiKey)
            .Matches(SendGridProviderSettingConstant.Patterns.ApiKey)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.ApiKey))
            .WithErrorCode(SendGridProviderSettingOutcome.Failure.ApiKeyInvalidFormat.Code)
            .WithMessage(SendGridProviderSettingOutcome.Failure.ApiKeyInvalidFormat.Message);

        // Validate: Reject whitespace characters in API key
        RuleFor(x => x.ApiKey)
            .Must(key => !key.Any(char.IsWhiteSpace))
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.ApiKey))
            .WithErrorCode(SendGridProviderSettingOutcome.Failure.ApiKeyContainsWhitespace.Code)
            .WithMessage(SendGridProviderSettingOutcome.Failure.ApiKeyContainsWhitespace.Message);
    }
}

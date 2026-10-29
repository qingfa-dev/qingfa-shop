using FluentValidation;

namespace Application.Common.Integrations.Notifications.Channels.Emails.Providers.SendGrid;

/// <summary>Validates SendGrid provider configuration settings.</summary>
public sealed class SendGridProviderSettingValidator : AbstractValidator<SendGridProviderSetting>
{
    public SendGridProviderSettingValidator()
    {
        // Validate: Ensure API key is provided when provider is enabled
        RuleFor(x => x.ApiKey)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SendGridProviderSettingResult.Failure.ApiKeyRequired.Code)
            .WithMessage(SendGridProviderSettingResult.Failure.ApiKeyRequired.Message);

        // Validate: Enforce minimum API key length
        RuleFor(x => x.ApiKey)
            .MinimumLength(SendGridProviderSettingConstant.Constraints.ApiKeyMinLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.ApiKey))
            .WithErrorCode(SendGridProviderSettingResult.Failure.ApiKeyTooShort.Code)
            .WithMessage(SendGridProviderSettingResult.Failure.ApiKeyTooShort.Message);

        // Validate: Enforce maximum API key length
        RuleFor(x => x.ApiKey)
            .MaximumLength(SendGridProviderSettingConstant.Constraints.ApiKeyMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.ApiKey))
            .WithErrorCode(SendGridProviderSettingResult.Failure.ApiKeyTooLong.Code)
            .WithMessage(SendGridProviderSettingResult.Failure.ApiKeyTooLong.Message);

        // Validate: Verify API key matches expected format (SG.xxx.xxx)
        RuleFor(x => x.ApiKey)
            .Matches(SendGridProviderSettingConstant.Patterns.ApiKey)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.ApiKey))
            .WithErrorCode(SendGridProviderSettingResult.Failure.ApiKeyInvalidFormat.Code)
            .WithMessage(SendGridProviderSettingResult.Failure.ApiKeyInvalidFormat.Message);

        // Validate: Reject whitespace characters in API key
        RuleFor(x => x.ApiKey)
            .Must(key => !key.Any(char.IsWhiteSpace))
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.ApiKey))
            .WithErrorCode(SendGridProviderSettingResult.Failure.ApiKeyContainsWhitespace.Code)
            .WithMessage(SendGridProviderSettingResult.Failure.ApiKeyContainsWhitespace.Message);
    }
}

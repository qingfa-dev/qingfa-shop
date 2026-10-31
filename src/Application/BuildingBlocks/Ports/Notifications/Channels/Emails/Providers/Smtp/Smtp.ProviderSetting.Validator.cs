using FluentValidation;

namespace Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Providers.Smtp;

/// <summary>Validates SMTP provider configuration settings.</summary>
public sealed class SmtpProviderSettingValidator : AbstractValidator<SmtpProviderSetting>
{
    public SmtpProviderSettingValidator()
    {
        // Validate: Ensure SMTP host is provided when enabled
        RuleFor(x => x.Host)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SmtpProviderSettingOutcome.Failure.SmtpHostRequired.Code)
            .WithMessage(SmtpProviderSettingOutcome.Failure.SmtpHostRequired.Message);

        // Validate: Enforce maximum host length
        RuleFor(x => x.Host)
            .MaximumLength(SmtpProviderSettingConstant.Constraints.HostMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.Host))
            .WithErrorCode(SmtpProviderSettingOutcome.Failure.SmtpHostTooLong.Code)
            .WithMessage(SmtpProviderSettingOutcome.Failure.SmtpHostTooLong.Message);

        // Validate: Verify hostname format is valid
        RuleFor(x => x.Host)
            .Must(host => SmtpProviderSettingConstant.Patterns.HostName.IsMatch(host))
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.Host))
            .WithErrorCode(SmtpProviderSettingOutcome.Failure.SmtpHostInvalidFormat.Code)
            .WithMessage(SmtpProviderSettingOutcome.Failure.SmtpHostInvalidFormat.Message);

        // Validate: Ensure port is above minimum value
        RuleFor(x => x.Port)
            .GreaterThan(SmtpProviderSettingConstant.Constraints.PortMin)
            .When(x => x.Enabled)
            .WithErrorCode(SmtpProviderSettingOutcome.Failure.SmtpPortInvalid.Code)
            .WithMessage(SmtpProviderSettingOutcome.Failure.SmtpPortInvalid.Message);

        // Validate: Ensure port is below maximum value
        RuleFor(x => x.Port)
            .LessThan(SmtpProviderSettingConstant.Constraints.PortMax)
            .When(x => x.Enabled)
            .WithErrorCode(SmtpProviderSettingOutcome.Failure.SmtpPortOutOfRange.Code)
            .WithMessage(SmtpProviderSettingOutcome.Failure.SmtpPortOutOfRange.Message);

        // Validate: Ensure username is provided when not using default credentials
        RuleFor(x => x.Username)
            .NotEmpty()
            .When(x => x.Enabled && !x.UseDefaultCredentials)
            .WithErrorCode(SmtpProviderSettingOutcome.Failure.SmtpCredentialsRequired.Code)
            .WithMessage(SmtpProviderSettingOutcome.Failure.SmtpCredentialsRequired.Message);

        // Validate: Enforce maximum username length
        RuleFor(x => x.Username)
            .MaximumLength(SmtpProviderSettingConstant.Constraints.UsernameMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.Username))
            .WithErrorCode(SmtpProviderSettingOutcome.Failure.SmtpUsernameTooLong.Code)
            .WithMessage(SmtpProviderSettingOutcome.Failure.SmtpUsernameTooLong.Message);

        // Validate: Ensure password is provided when username is set
        RuleFor(x => x.Password)
            .NotEmpty()
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.Username))
            .WithErrorCode(SmtpProviderSettingOutcome.Failure.SmtpPasswordRequired.Code)
            .WithMessage(SmtpProviderSettingOutcome.Failure.SmtpPasswordRequired.Message);

        // Validate: Enforce maximum password length
        RuleFor(x => x.Password)
            .MaximumLength(SmtpProviderSettingConstant.Constraints.PasswordMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.Password))
            .WithErrorCode(SmtpProviderSettingOutcome.Failure.SmtpPasswordTooLong.Code)
            .WithMessage(SmtpProviderSettingOutcome.Failure.SmtpPasswordTooLong.Message);
    }
}

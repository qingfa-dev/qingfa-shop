using FluentValidation;

namespace Application.Common.Integrations.Notifications.Channels.Emails.Providers.Smtp;

/// <summary>Validates SMTP provider configuration settings.</summary>
public sealed class SmtpProviderSettingValidator : AbstractValidator<SmtpProviderSetting>
{
    public SmtpProviderSettingValidator()
    {
        // Validate: Ensure SMTP host is provided when enabled
        RuleFor(x => x.Host)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SmtpProviderSettingResult.Failure.SmtpHostRequired.Code)
            .WithMessage(SmtpProviderSettingResult.Failure.SmtpHostRequired.Message);

        // Validate: Enforce maximum host length
        RuleFor(x => x.Host)
            .MaximumLength(SmtpProviderSettingConstant.Constraints.HostMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.Host))
            .WithErrorCode(SmtpProviderSettingResult.Failure.SmtpHostTooLong.Code)
            .WithMessage(SmtpProviderSettingResult.Failure.SmtpHostTooLong.Message);

        // Validate: Verify hostname format is valid
        RuleFor(x => x.Host)
            .Must(host => SmtpProviderSettingConstant.Patterns.HostName.IsMatch(host))
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.Host))
            .WithErrorCode(SmtpProviderSettingResult.Failure.SmtpHostInvalidFormat.Code)
            .WithMessage(SmtpProviderSettingResult.Failure.SmtpHostInvalidFormat.Message);

        // Validate: Ensure port is above minimum value
        RuleFor(x => x.Port)
            .GreaterThan(SmtpProviderSettingConstant.Constraints.PortMin)
            .When(x => x.Enabled)
            .WithErrorCode(SmtpProviderSettingResult.Failure.SmtpPortInvalid.Code)
            .WithMessage(SmtpProviderSettingResult.Failure.SmtpPortInvalid.Message);

        // Validate: Ensure port is below maximum value
        RuleFor(x => x.Port)
            .LessThan(SmtpProviderSettingConstant.Constraints.PortMax)
            .When(x => x.Enabled)
            .WithErrorCode(SmtpProviderSettingResult.Failure.SmtpPortOutOfRange.Code)
            .WithMessage(SmtpProviderSettingResult.Failure.SmtpPortOutOfRange.Message);

        // Validate: Ensure username is provided when not using default credentials
        RuleFor(x => x.Username)
            .NotEmpty()
            .When(x => x.Enabled && !x.UseDefaultCredentials)
            .WithErrorCode(SmtpProviderSettingResult.Failure.SmtpCredentialsRequired.Code)
            .WithMessage(SmtpProviderSettingResult.Failure.SmtpCredentialsRequired.Message);

        // Validate: Enforce maximum username length
        RuleFor(x => x.Username)
            .MaximumLength(SmtpProviderSettingConstant.Constraints.UsernameMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.Username))
            .WithErrorCode(SmtpProviderSettingResult.Failure.SmtpUsernameTooLong.Code)
            .WithMessage(SmtpProviderSettingResult.Failure.SmtpUsernameTooLong.Message);

        // Validate: Ensure password is provided when username is set
        RuleFor(x => x.Password)
            .NotEmpty()
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.Username))
            .WithErrorCode(SmtpProviderSettingResult.Failure.SmtpPasswordRequired.Code)
            .WithMessage(SmtpProviderSettingResult.Failure.SmtpPasswordRequired.Message);

        // Validate: Enforce maximum password length
        RuleFor(x => x.Password)
            .MaximumLength(SmtpProviderSettingConstant.Constraints.PasswordMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.Password))
            .WithErrorCode(SmtpProviderSettingResult.Failure.SmtpPasswordTooLong.Code)
            .WithMessage(SmtpProviderSettingResult.Failure.SmtpPasswordTooLong.Message);
    }
}

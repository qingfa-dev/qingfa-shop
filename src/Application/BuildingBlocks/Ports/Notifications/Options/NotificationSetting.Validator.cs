using Application.BuildingBlocks.Ports.Notifications.Options.Extensions;

using FluentValidation;



namespace Application.BuildingBlocks.Ports.Notifications.Options;

/// <summary>Validates the root notification system configuration settings.</summary>
public sealed class NotificationSettingValidator : AbstractValidator<NotificationSetting>
{
    public NotificationSettingValidator()
    {
        // Validate: ApplicationName must not be empty
        RuleFor(x => x.ApplicationName)
            .NotEmpty()
            .WithErrorCode(NotificationSettingOutcome.Failure.ApplicationNameRequired.Code)
            .WithMessage(NotificationSettingOutcome.Failure.ApplicationNameRequired.Message);

        // Validate: SupportEmail must be a valid email address format
        RuleFor(x => x.SupportEmail)
            .MustBeValidEmail()
                .WithErrorCode(NotificationSettingOutcome.Failure.InvalidSupportEmail.Code)
                .WithMessage(NotificationSettingOutcome.Failure.InvalidSupportEmail.Message);

        // Validate: SupportPhone must match the configured phone number pattern
        RuleFor(x => x.SupportPhone)
            .MustBeValidPhone()
                .WithErrorCode(NotificationSettingOutcome.Failure.InvalidSupportPhone.Code)
                .WithMessage(NotificationSettingOutcome.Failure.InvalidSupportPhone.Message);

        // Validate: ApplicationUrl must be a valid absolute URL
        RuleFor(x => x.ApplicationUrl)
            .MustBeValidUrl()
                .WithErrorCode(NotificationSettingOutcome.Failure.InvalidApplicationUrl.Code)
                .WithMessage(NotificationSettingOutcome.Failure.InvalidApplicationUrl.Message);

        // Validate: CustomerSupportLink must be a valid absolute URL
        RuleFor(x => x.CustomerSupportLink)
            .MustBeValidUrl()
                .WithErrorCode(NotificationSettingOutcome.Failure.InvalidCustomerSupportLink.Code)
                .WithMessage(NotificationSettingOutcome.Failure.InvalidCustomerSupportLink.Message);

        // Validate: UnsubscribeUrl must be a valid absolute URL
        RuleFor(x => x.UnsubscribeUrl)
            .MustBeValidUrl()
                .WithErrorCode(NotificationSettingOutcome.Failure.InvalidUnsubscribeUrl.Code)
                .WithMessage(NotificationSettingOutcome.Failure.InvalidUnsubscribeUrl.Message);

        // Validate: SurveyUrl must be a valid absolute URL
        RuleFor(x => x.SurveyUrl)
            .MustBeValidUrl()
                .WithErrorCode(NotificationSettingOutcome.Failure.InvalidSurveyUrl.Code)
                .WithMessage(NotificationSettingOutcome.Failure.InvalidSurveyUrl.Message);
    }
}

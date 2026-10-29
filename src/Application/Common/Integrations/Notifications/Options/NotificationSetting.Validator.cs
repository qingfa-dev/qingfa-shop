using Application.Common.Integrations.Notifications.Options.Extensions;

using FluentValidation;



namespace Application.Common.Integrations.Notifications.Options;

/// <summary>Validates the root notification system configuration settings.</summary>
public sealed class NotificationSettingValidator : AbstractValidator<NotificationSetting>
{
    public NotificationSettingValidator()
    {
        // Validate: ApplicationName must not be empty
        RuleFor(x => x.ApplicationName)
            .NotEmpty()
            .WithErrorCode(NotificationSettingResult.Failure.ApplicationNameRequired.Code)
            .WithMessage(NotificationSettingResult.Failure.ApplicationNameRequired.Message);

        // Validate: SupportEmail must be a valid email address format
        RuleFor(x => x.SupportEmail)
            .MustBeValidEmail()
                .WithErrorCode(NotificationSettingResult.Failure.InvalidSupportEmail.Code)
                .WithMessage(NotificationSettingResult.Failure.InvalidSupportEmail.Message);

        // Validate: SupportPhone must match the configured phone number pattern
        RuleFor(x => x.SupportPhone)
            .MustBeValidPhone()
                .WithErrorCode(NotificationSettingResult.Failure.InvalidSupportPhone.Code)
                .WithMessage(NotificationSettingResult.Failure.InvalidSupportPhone.Message);

        // Validate: ApplicationUrl must be a valid absolute URL
        RuleFor(x => x.ApplicationUrl)
            .MustBeValidUrl()
                .WithErrorCode(NotificationSettingResult.Failure.InvalidApplicationUrl.Code)
                .WithMessage(NotificationSettingResult.Failure.InvalidApplicationUrl.Message);

        // Validate: CustomerSupportLink must be a valid absolute URL
        RuleFor(x => x.CustomerSupportLink)
            .MustBeValidUrl()
                .WithErrorCode(NotificationSettingResult.Failure.InvalidCustomerSupportLink.Code)
                .WithMessage(NotificationSettingResult.Failure.InvalidCustomerSupportLink.Message);

        // Validate: UnsubscribeUrl must be a valid absolute URL
        RuleFor(x => x.UnsubscribeUrl)
            .MustBeValidUrl()
                .WithErrorCode(NotificationSettingResult.Failure.InvalidUnsubscribeUrl.Code)
                .WithMessage(NotificationSettingResult.Failure.InvalidUnsubscribeUrl.Message);

        // Validate: SurveyUrl must be a valid absolute URL
        RuleFor(x => x.SurveyUrl)
            .MustBeValidUrl()
                .WithErrorCode(NotificationSettingResult.Failure.InvalidSurveyUrl.Code)
                .WithMessage(NotificationSettingResult.Failure.InvalidSurveyUrl.Message);
    }
}

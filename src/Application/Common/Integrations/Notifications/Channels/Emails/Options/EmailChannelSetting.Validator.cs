using FluentValidation;

using Application.Common.Integrations.Notifications.Options.Extensions;

namespace Application.Common.Integrations.Notifications.Channels.Emails.Options;

/// <summary>
/// Fluent validator for <see cref="EmailChannelSetting"/>.
/// </summary>
public sealed class EmailChannelSettingValidator : AbstractValidator<EmailChannelSetting>
{
    public EmailChannelSettingValidator()
    {
        RuleFor(x => x.FromEmail)
        .MustBeValidEmail()
        .WithErrorCode(EmailChannelSettingResult.Failure.FromEmailRequired.Code)
        .WithMessage(EmailChannelSettingResult.Failure.FromEmailRequired.Message);

        RuleFor(x => x.FromName)
        .NotEmpty()
        .WithErrorCode(EmailChannelSettingResult.Failure.FromNameRequired.Code)
        .WithMessage(EmailChannelSettingResult.Failure.FromNameRequired.Message);
    }
}

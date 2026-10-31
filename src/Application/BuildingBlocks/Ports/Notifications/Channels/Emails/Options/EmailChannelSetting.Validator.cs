using FluentValidation;

using Application.BuildingBlocks.Ports.Notifications.Options.Extensions;

namespace Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Options;

/// <summary>
/// Fluent validator for <see cref="EmailChannelSetting"/>.
/// </summary>
public sealed class EmailChannelSettingValidator : AbstractValidator<EmailChannelSetting>
{
    public EmailChannelSettingValidator()
    {
        RuleFor(x => x.FromEmail)
        .MustBeValidEmail()
        .WithErrorCode(EmailChannelSettingOutcome.Failure.FromEmailRequired.Code)
        .WithMessage(EmailChannelSettingOutcome.Failure.FromEmailRequired.Message);

        RuleFor(x => x.FromName)
        .NotEmpty()
        .WithErrorCode(EmailChannelSettingOutcome.Failure.FromNameRequired.Code)
        .WithMessage(EmailChannelSettingOutcome.Failure.FromNameRequired.Message);
    }
}

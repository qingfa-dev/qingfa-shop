using System.Text.RegularExpressions;

using FluentValidation;

using Application.BuildingBlocks.Ports.Notifications.Options;

namespace Application.BuildingBlocks.Ports.Notifications.Channels.Sms.Options;

/// <summary>Validates SMS channel configuration settings.</summary>
public sealed class SmsChannelSettingValidator : AbstractValidator<SmsChannelSetting>
{
    public SmsChannelSettingValidator()
    {
        RuleFor(x => x.DefaultSenderNumber)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SmsChannelSettingOutcome.Failure.DefaultSenderNumberRequired.Code)
            .WithMessage(SmsChannelSettingOutcome.Failure.DefaultSenderNumberRequired.Message);

        RuleFor(x => x.DefaultSenderNumber)
            .Matches(
                new Regex(
                    NotificationSettingConstant.Patterns.PhoneNumber,
                    RegexOptions.None,
                    TimeSpan.FromMilliseconds(NotificationSettingConstant.Constraints.RegexTimeoutMilliseconds)))
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.DefaultSenderNumber))
            .WithErrorCode(SmsChannelSettingOutcome.Failure.DefaultSenderNumberInvalid.Code)
            .WithMessage(SmsChannelSettingOutcome.Failure.DefaultSenderNumberInvalid.Message);

        RuleFor(x => x.DefaultSenderNumber)
            .MaximumLength(SmsChannelSettingConstant.Constraints.DefaultSenderNumberMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.DefaultSenderNumber))
            .WithErrorCode(SmsChannelSettingOutcome.Failure.DefaultSenderNumberInvalid.Code)
            .WithMessage(SmsChannelSettingOutcome.Failure.DefaultSenderNumberInvalid.Message);
    }
}

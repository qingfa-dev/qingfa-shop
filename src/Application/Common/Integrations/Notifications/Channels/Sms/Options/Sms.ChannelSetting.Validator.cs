using System.Text.RegularExpressions;

using FluentValidation;

using Application.Common.Integrations.Notifications.Options;

namespace Application.Common.Integrations.Notifications.Channels.Sms.Options;

/// <summary>Validates SMS channel configuration settings.</summary>
public sealed class SmsChannelSettingValidator : AbstractValidator<SmsChannelSetting>
{
    public SmsChannelSettingValidator()
    {
        RuleFor(x => x.DefaultSenderNumber)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithErrorCode(SmsChannelSettingResult.Failure.DefaultSenderNumberRequired.Code)
            .WithMessage(SmsChannelSettingResult.Failure.DefaultSenderNumberRequired.Message);

        RuleFor(x => x.DefaultSenderNumber)
            .Matches(
                new Regex(
                    NotificationSettingConstant.Patterns.PhoneNumber,
                    RegexOptions.None,
                    TimeSpan.FromMilliseconds(100)))
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.DefaultSenderNumber))
            .WithErrorCode(SmsChannelSettingResult.Failure.DefaultSenderNumberInvalid.Code)
            .WithMessage(SmsChannelSettingResult.Failure.DefaultSenderNumberInvalid.Message);

        RuleFor(x => x.DefaultSenderNumber)
            .MaximumLength(SmsChannelSettingConstant.Constraints.DefaultSenderNumberMaxLength)
            .When(x => x.Enabled && !string.IsNullOrEmpty(x.DefaultSenderNumber))
            .WithErrorCode(SmsChannelSettingResult.Failure.DefaultSenderNumberInvalid.Code)
            .WithMessage(SmsChannelSettingResult.Failure.DefaultSenderNumberInvalid.Message);
    }
}

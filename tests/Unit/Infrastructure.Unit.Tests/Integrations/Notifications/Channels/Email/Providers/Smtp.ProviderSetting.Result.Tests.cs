namespace Infrastructure.Unit.Tests.Integrations.Notifications.Channels.Email.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SmtpProviderSettingResultTests
{
    public static IEnumerable<object[]> ErrorData()
    {
        yield return
        [
            SmtpProviderSettingResult.Failure.SmtpHostRequired, "Notification.Email.Smtp.Host.Required",
            "SMTP host is required when the provider is enabled.", 422
        ];
        yield return
        [
            SmtpProviderSettingResult.Failure.SmtpHostTooLong, "Notification.Email.Smtp.Host.TooLong",
            "SMTP host must not exceed 256 characters.", 422
        ];
        yield return
        [
            SmtpProviderSettingResult.Failure.SmtpHostInvalidFormat, "Notification.Email.Smtp.Host.InvalidFormat",
            "SMTP host must be a valid hostname (e.g., smtp.example.com), IP address, or 'localhost'.",
            422
        ];
        yield return
        [
            SmtpProviderSettingResult.Failure.SmtpPortInvalid, "Notification.Email.Smtp.Port.Invalid",
            "SMTP port must be a positive integer greater than 1.", 422
        ];
        yield return
        [
            SmtpProviderSettingResult.Failure.SmtpPortOutOfRange, "Notification.Email.Smtp.Port.OutOfRange",
            "SMTP port must be between 1 and 65535.", 422
        ];
        yield return
        [
            SmtpProviderSettingResult.Failure.SmtpCredentialsRequired, "Notification.Email.Smtp.Credentials.Required",
            "SMTP username is required when not using default network credentials.", 422
        ];
        yield return
        [
            SmtpProviderSettingResult.Failure.SmtpPasswordRequired, "Notification.Email.Smtp.Password.Required",
            "SMTP password is required when a username is provided.", 422
        ];
    }

    [Theory(DisplayName = "Error '{0}' should return expected Code, Message, and Type")]
    [MemberData(nameof(ErrorData))]
    public void Error_ShouldReturnExpectedValues(Error error, string expectedCode, string expectedMessage,
        int expectedType)
    {
        error.Code.Should().Be(expectedCode);
        error.Message.Should().Be(expectedMessage);
        error.Status.Should().Be(expectedType);
    }
}
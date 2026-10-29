namespace Infrastructure.Unit.Tests.Integrations.Notifications.Channels.Sms.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SmsChannelSettingResultTests
{
    public static IEnumerable<object[]> StaticErrorData()
    {
        yield return
        [
            SmsChannelSettingResult.Failure.ToRequired, "Notification.Sms.ToRequired",
            "At least one recipient is required.", 422
        ];
        yield return
        [
            SmsChannelSettingResult.Failure.InvalidFormat, "Notification.Sms.InvalidFormat",
            "Invalid phone number format.", 422
        ];
        yield return
        [
            SmsChannelSettingResult.Failure.BodyRequired, "Notification.Sms.BodyRequired", "SMS body is required.",
            422
        ];
        yield return
        [
            SmsChannelSettingResult.Failure.BodyTooLong, "Notification.Sms.BodyTooLong",
            "SMS body cannot exceed 160 characters.", 422
        ];
        yield return
        [
            SmsChannelSettingResult.Failure.NoProvidersConfigured, "Notification.Sms.NoProvidersConfigured",
            "No active SMS providers are configured.", 500
        ];
        yield return
        [
            SmsChannelSettingResult.Failure.AllProvidersFailed, "Notification.Sms.AllProvidersFailed",
            "All SMS providers failed to send the notification.", 500
        ];
        yield return
        [
            SmsChannelSettingResult.Failure.DefaultSenderNumberRequired,
            "Notification.Sms.DefaultSenderNumber.Required", "Default sender number is required.", 422
        ];
        yield return
        [
            SmsChannelSettingResult.Failure.DefaultSenderNumberInvalid,
            "Notification.Sms.DefaultSenderNumber.InvalidFormat",
            "Default sender number must be in E.164 format (e.g., +1234567890).", 422
        ];
    }

    [Theory(DisplayName = "Static error should return expected Code, Message, and Type")]
    [MemberData(nameof(StaticErrorData))]
    public void StaticError_ShouldReturnExpectedValues(Error error, string expectedCode, string expectedMessage,
        int expectedType)
    {
        error.Code.Should().Be(expectedCode);
        error.Message.Should().Be(expectedMessage);
        error.Status.Should().Be(expectedType);
    }

    [Fact(DisplayName = "Failure.SendFailed should include details in message")]
    public void Failure_SendFailed_ShouldIncludeDetails()
    {
        Error error = SmsChannelSettingResult.Failure.SendFailed("test-details");

        error.Code.Should().Be("Notification.Sms.SendFailed");
        error.Message.Should().Be("Failed to send SMS: test-details");
        error.Status.Should().Be(500);
    }
}
namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Channels.Email.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SendGridProviderSettingResultTests
{
    public static IEnumerable<object[]> ErrorData()
    {
        yield return
        [
            SendGridProviderSettingOutcome.Failure.ApiKeyRequired, "Notification.Email.SendGrid.ApiKey.Required",
            "SendGrid API key is required when the provider is enabled.", 422
        ];
        yield return
        [
            SendGridProviderSettingOutcome.Failure.ApiKeyTooShort, "Notification.Email.SendGrid.ApiKey.TooShort",
            "SendGrid API key must be at least 10 characters.", 422
        ];
        yield return
        [
            SendGridProviderSettingOutcome.Failure.ApiKeyTooLong, "Notification.Email.SendGrid.ApiKey.TooLong",
            "SendGrid API key must not exceed 256 characters.", 422
        ];
        yield return
        [
            SendGridProviderSettingOutcome.Failure.ApiKeyInvalidFormat,
            "Notification.Email.SendGrid.ApiKey.InvalidFormat",
            "SendGrid API key must start with 'SG.' followed by two dot-separated segments of alphanumeric characters.",
            422
        ];
        yield return
        [
            SendGridProviderSettingOutcome.Failure.ApiKeyContainsWhitespace,
            "Notification.Email.SendGrid.ApiKey.ContainsWhitespace",
            "SendGrid API key must not contain whitespace characters.", 422
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
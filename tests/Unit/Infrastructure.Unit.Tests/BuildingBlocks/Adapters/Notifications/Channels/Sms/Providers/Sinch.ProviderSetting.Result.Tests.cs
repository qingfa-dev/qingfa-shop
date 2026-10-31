namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Channels.Sms.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SinchProviderSettingResultTests
{
    public static IEnumerable<object[]> ErrorData()
    {
        yield return
        [
            SinchProviderSettingOutcome.Failure.ProjectIdRequired, "Notification.Sms.Sinch.ProjectId.Required",
            "Sinch Project ID is required when the provider is enabled.", 422
        ];
        yield return
        [
            SinchProviderSettingOutcome.Failure.KeyIdRequired, "Notification.Sms.Sinch.KeyId.Required",
            "Sinch Key ID is required when the provider is enabled.", 422
        ];
        yield return
        [
            SinchProviderSettingOutcome.Failure.KeySecretRequired, "Notification.Sms.Sinch.KeySecret.Required",
            "Sinch Key Secret is required when the provider is enabled.", 422
        ];
        yield return
        [
            SinchProviderSettingOutcome.Failure.SenderPhoneNumberRequired,
            "Notification.Sms.Sinch.SenderPhoneNumber.Required",
            "Sinch Sender Phone Number is required when the provider is enabled.", 422
        ];
        yield return
        [
            SinchProviderSettingOutcome.Failure.ProjectIdInvalidLength, "Notification.Sms.Sinch.ProjectId.InvalidLength",
            "Sinch Project ID must be between 1 and 64 characters.", 422
        ];
        yield return
        [
            SinchProviderSettingOutcome.Failure.KeyIdInvalidLength, "Notification.Sms.Sinch.KeyId.InvalidLength",
            "Sinch Key ID must be between 1 and 128 characters.", 422
        ];
        yield return
        [
            SinchProviderSettingOutcome.Failure.KeySecretInvalidLength, "Notification.Sms.Sinch.KeySecret.InvalidLength",
            "Sinch Key Secret must be between 1 and 256 characters.", 422
        ];
        yield return
        [
            SinchProviderSettingOutcome.Failure.SenderPhoneNumberInvalid,
            "Notification.Sms.Sinch.SenderPhoneNumber.InvalidFormat",
            "Sender phone number must be in E.164 format (e.g., +1234567890).", 422
        ];
    }

    [Theory(DisplayName = "Error should return expected Code, Message, and Type")]
    [MemberData(nameof(ErrorData))]
    public void Error_ShouldReturnExpectedValues(Error error, string expectedCode, string expectedMessage,
        int expectedType)
    {
        error.Code.Should().Be(expectedCode);
        error.Message.Should().Be(expectedMessage);
        error.Status.Should().Be(expectedType);
    }
}
using Microsoft.Extensions.Logging;

using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Channels.Logging.Providers;
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class LoggingProviderBehaviorTests
{
    private static NotificationMessage CreateValidSmsMessage()
    {
        return NotificationMessage.Create(
            NotificationUseCase.PasswordSetupRequested,
            NotificationRecipient.Create("+1234567890"),
            NotificationChannel.SMS,
            NotificationContext.Create(
                (NotificationParameterType.UserFirstName, "Jane"),
                (NotificationParameterType.VerificationCode, "123456"),
                (NotificationParameterType.ApplicationName, "TestSystem"),
                (NotificationParameterType.SupportPhone, "+1234567890")));
    }
    private static NotificationMessage CreateValidEmailMessage()
    {
        return NotificationMessage.Create(
            NotificationUseCase.UserRegistered,
            NotificationRecipient.Create("test@test.com"),
            NotificationChannel.Email,
            NotificationContext.Create(
                (NotificationParameterType.UserFirstName, "Jane"),
                (NotificationParameterType.VerificationUrl, "https://example.com/activate"),
                (NotificationParameterType.ApplicationName, "TestSystem"),
                (NotificationParameterType.SupportEmail, "support@test.com"),
                (NotificationParameterType.UnsubscribeUrl, "https://example.com/unsubscribe")));
    }
    [Theory(DisplayName = "SendAsync should log and return Ok for multiple channels")]
    [MemberData(nameof(ChannelData))]
    public async Task SendAsync_ShouldLogAndReturnOk(NotificationChannel channel, NotificationMessage message)
    {
        Mock<ILogger<LoggingProvider>> loggerMock = new();
        LoggingProvider provider = new(loggerMock.Object, channel);
        Result result = await provider.SendAsync(message);
        result.IsSuccess.Should().BeTrue();
    }
    public static IEnumerable<object[]> ChannelData()
    {
        yield return [NotificationChannel.SMS, CreateValidSmsMessage()];
        yield return [NotificationChannel.Email, CreateValidEmailMessage()];
    }
}

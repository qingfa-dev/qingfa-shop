using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Hubs;
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationHubResultTests
{
    public static IEnumerable<object[]> ErrorData()
    {
        yield return
        [
            NotificationHubOutcome.Failure.NoProvidersConfigured(NotificationChannel.SMS),
            "Notification.SMS.NoProvidersConfigured",
            "No active SMS providers are configured.",
            500
        ];
        yield return
        [
            NotificationHubOutcome.Failure.NoProvidersConfigured(NotificationChannel.Email),
            "Notification.Email.NoProvidersConfigured",
            "No active Email providers are configured.",
            500
        ];
        yield return
        [
            NotificationHubOutcome.Failure.AllProvidersFailed(NotificationChannel.Email),
            "Notification.Email.AllProvidersFailed",
            "All Email providers failed.",
            500
        ];
        yield return
        [
            NotificationHubOutcome.Failure.AllProvidersFailed(NotificationChannel.SMS),
            "Notification.SMS.AllProvidersFailed",
            "All SMS providers failed.",
            500
        ];
    }
    [Theory(DisplayName = "Error factory should return correct Code, Message, and Type")]
    [MemberData(nameof(ErrorData))]
    public void ErrorFactory_ShouldReturnCorrectProperties(Error error, string expectedCode, string expectedMessage, int expectedType)
    {
        error.Code.Should().Be(expectedCode);
        error.Message.Should().Be(expectedMessage);
        error.Status.Should().Be(expectedType);
    }
}

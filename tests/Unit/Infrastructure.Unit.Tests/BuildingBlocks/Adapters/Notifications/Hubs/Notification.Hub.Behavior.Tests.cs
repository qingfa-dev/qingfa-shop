using Microsoft.Extensions.Logging;

using Application.BuildingBlocks.Ports.Notifications.Providers;
using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Hubs;
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationHubBehaviorTests
{
    private static Mock<INotificationProvider> CreateProviderMock(
        string name,
        NotificationChannel channel,
        int priority,
        bool isEnabled,
        Result result)
    {
        Mock<INotificationProvider> mock = new();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.Channel).Returns(channel);
        mock.Setup(p => p.Priority).Returns(priority);
        mock.Setup(p => p.IsEnabled).Returns(isEnabled);
        mock.Setup(p => p.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return mock;
    }
    private static NotificationMessage CreateTestMessage(NotificationChannel channel = NotificationChannel.Email)
    {
        return NotificationMessage.Create(
            NotificationUseCase.UserRegistered,
            NotificationRecipient.Create("test@test.com", "Test"),
            channel,
            NotificationContext.Empty);
    }
    [Fact(DisplayName = "SendAsync when first provider succeeds should return Ok")]
    public async Task SendAsync_FirstProviderSucceeds_ShouldReturnOk()
    {
        Mock<INotificationProvider> successProvider = CreateProviderMock("Success", NotificationChannel.Email, 1, true, Result.Ok());
        Mock<INotificationProvider> failProvider = CreateProviderMock("Fail", NotificationChannel.Email, 2, true, Result.Fail(Error.Unexpected("Test.Fail", "fail")));
        NotificationHub hub = new(
            [successProvider.Object, failProvider.Object],
            Mock.Of<ILogger<NotificationHub>>());
        Result result = await hub.SendAsync(CreateTestMessage());
        result.IsSuccess.Should().BeTrue();
    }
    [Fact(DisplayName = "SendAsync when first provider fails should fallback to next")]
    public async Task SendAsync_FirstProviderFails_ShouldFallbackToNext()
    {
        Mock<INotificationProvider> failProvider = CreateProviderMock("Fail", NotificationChannel.Email, 1, true, Result.Fail(Error.Unexpected("Test.Fail", "first failed")));
        Mock<INotificationProvider> successProvider = CreateProviderMock("Success", NotificationChannel.Email, 2, true, Result.Ok());
        NotificationHub hub = new(
            [failProvider.Object, successProvider.Object],
            Mock.Of<ILogger<NotificationHub>>());
        Result result = await hub.SendAsync(CreateTestMessage());
        result.IsSuccess.Should().BeTrue();
    }
    [Fact(DisplayName = "SendAsync when all providers fail should return AllProvidersFailed")]
    public async Task SendAsync_AllProvidersFail_ShouldReturnAllProvidersFailed()
    {
        Mock<INotificationProvider> first = CreateProviderMock("First", NotificationChannel.Email, 1, true, Result.Fail(Error.Unexpected("Test.Fail", "error1")));
        Mock<INotificationProvider> second = CreateProviderMock("Second", NotificationChannel.Email, 2, true, Result.Fail(Error.Unexpected("Test.Fail", "error2")));
        NotificationHub hub = new(
            [first.Object, second.Object],
            Mock.Of<ILogger<NotificationHub>>());
        Result result = await hub.SendAsync(CreateTestMessage());
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code.Contains("AllProvidersFailed"));
    }
    [Fact(DisplayName = "SendAsync when no active providers should return NoProvidersConfigured")]
    public async Task SendAsync_NoActiveProviders_ShouldReturnNoProvidersConfigured()
    {
        Mock<INotificationProvider> disabledProvider = CreateProviderMock("Disabled", NotificationChannel.Email, 1, false, Result.Ok());
        NotificationHub hub = new(
            [disabledProvider.Object],
            Mock.Of<ILogger<NotificationHub>>());
        Result result = await hub.SendAsync(CreateTestMessage());
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code.Contains("NoProvidersConfigured"));
    }
    [Fact(DisplayName = "SendAsync should skip providers of different channel")]
    public async Task SendAsync_SkipsProvidersOfDifferentChannel()
    {
        Mock<INotificationProvider> smsProvider = CreateProviderMock("SmsProvider", NotificationChannel.SMS, 1, true, Result.Ok());
        Mock<INotificationProvider> emailProvider = CreateProviderMock("EmailProvider", NotificationChannel.Email, 2, true, Result.Ok());
        NotificationHub hub = new(
            [smsProvider.Object, emailProvider.Object],
            Mock.Of<ILogger<NotificationHub>>());
        Result result = await hub.SendAsync(CreateTestMessage(NotificationChannel.Email));
        result.IsSuccess.Should().BeTrue();
    }
    [Fact(DisplayName = "SendAsync should respect priority order")]
    public async Task SendAsync_RespectsPriorityOrder()
    {
        List<string> callOrder = [];
        Mock<INotificationProvider> lowPriority = new();
        lowPriority.Setup(p => p.Name).Returns("LowPriority");
        lowPriority.Setup(p => p.Channel).Returns(NotificationChannel.Email);
        lowPriority.Setup(p => p.Priority).Returns(10);
        lowPriority.Setup(p => p.IsEnabled).Returns(true);
        lowPriority.Setup(p => p.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(Error.Unexpected("Test.Fail", "fail")))
            .Callback(() => callOrder.Add("LowPriority"));
        Mock<INotificationProvider> highPriority = new();
        highPriority.Setup(p => p.Name).Returns("HighPriority");
        highPriority.Setup(p => p.Channel).Returns(NotificationChannel.Email);
        highPriority.Setup(p => p.Priority).Returns(1);
        highPriority.Setup(p => p.IsEnabled).Returns(true);
        highPriority.Setup(p => p.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(Error.Unexpected("Test.Fail", "fail")))
            .Callback(() => callOrder.Add("HighPriority"));
        NotificationHub hub = new(
            [lowPriority.Object, highPriority.Object],
            Mock.Of<ILogger<NotificationHub>>());
        await hub.SendAsync(CreateTestMessage());
        callOrder[0].Should().Be("HighPriority");
        callOrder[1].Should().Be("LowPriority");
    }
}

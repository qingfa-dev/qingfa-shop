using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Application.BuildingBlocks.Ports.Notifications.Options;
using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationServiceTests
{
    private readonly Mock<INotificationHub> _hubMock;
    private readonly Mock<IBackgroundJobClient> _jobClientMock;
    private readonly Mock<IOptions<NotificationSetting>> _optionsMock;
    private readonly NotificationSetting _settings;

    public NotificationServiceTests()
    {
        _hubMock = new Mock<INotificationHub>();
        _jobClientMock = new Mock<IBackgroundJobClient>();
        _optionsMock = new Mock<IOptions<NotificationSetting>>();
        _settings = new NotificationSetting { EnableBackgroundJobs = false };
        _optionsMock.Setup(x => x.Value).Returns(_settings);
    }

    private NotificationService CreateSut()
    {
        Mock<ILogger<NotificationService>> loggerMock = new();
        return new NotificationService(
            _hubMock.Object,
            _jobClientMock.Object,
            _optionsMock.Object,
            loggerMock.Object);
    }

    private static NotificationMessage CreateValidMessage()
    {
        return NotificationMessageBuilder.Create(
            NotificationUseCase.UserRegistered,
            NotificationRecipient.Create("user@test.com"),
            NotificationChannel.Email).Value;
    }

    [Fact(DisplayName = "SendAsync with valid message and hub success should return Ok")]
    public async Task SendAsync_WithValidMessageAndHubSuccess_ShouldReturnOk()
    {
        // Arrange: valid message, background jobs disabled, hub returns success
        NotificationMessage message = CreateValidMessage();
        NotificationService sut = CreateSut();

        _hubMock.Setup(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        Result result = await sut.SendAsync(message);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "SendAsync with valid message and hub failure should propagate errors")]
    public async Task SendAsync_WithValidMessageAndHubFailure_ShouldPropagateErrors()
    {
        // Arrange: valid message, hub returns an error
        NotificationMessage message = CreateValidMessage();
        NotificationService sut = CreateSut();

        Error expectedError = Error.Unexpected("Notification.Unexpected", "Provider failed");
        _hubMock.Setup(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedError);

        // Act
        Result result = await sut.SendAsync(message);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "SendAsync with unknown use case should return TemplateNotFound")]
    public async Task SendAsync_WithTemplateNotFound_ShouldReturnTemplateNotFound()
    {
        // Arrange: message with use case not in the template store
        NotificationMessage message = NotificationMessageBuilder.Create(
            NotificationUseCase.None,
            NotificationRecipient.Create("user@test.com"),
            NotificationChannel.Email).Value;
        NotificationService sut = CreateSut();

        // Act
        Result result = await sut.SendAsync(message);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Notification.TemplateNotFound");
    }

    [Fact(DisplayName = "SendAsync with background jobs enabled should enqueue and return Ok")]
    public async Task SendAsync_WithBackgroundJobsEnabled_ShouldEnqueueAndReturnOk()
    {
        // Arrange: enable background jobs, valid message
        _settings.EnableBackgroundJobs = true;
        NotificationMessage message = CreateValidMessage();
        NotificationService sut = CreateSut();

        // Act
        Result result = await sut.SendAsync(message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _jobClientMock.Verify(
            x => x.Create(
                It.IsAny<Job>(),
                It.IsAny<IState>()),
            Times.Once);
    }

    [Fact(DisplayName = "SendInternalAsync should apply defaults, map content, then call hub")]
    public async Task SendInternalAsync_ShouldApplyDefaultsThenMapContentThenCallHub()
    {
        // Arrange: message with valid use case, hub captures the dispatched message
        NotificationMessage message = CreateValidMessage();
        NotificationService sut = CreateSut();

        NotificationMessage? capturedMessage = null;
        _hubMock.Setup(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(Result.Ok());

        // Act
        Result result = await sut.SendInternalAsync(message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedMessage.Should().NotBeNull();
        _hubMock.Verify(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
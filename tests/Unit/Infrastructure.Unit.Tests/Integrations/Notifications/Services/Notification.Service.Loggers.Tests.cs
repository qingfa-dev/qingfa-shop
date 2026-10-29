using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Unit.Tests.Integrations.Notifications.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationServiceLoggerTests
{
    [Fact(DisplayName = "Loggers LogEnqueuingNotificationJob should compile")]
    public void Loggers_LogEnqueuingNotificationJob_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationService.Loggers.LogEnqueuingNotificationJob(logger, NotificationUseCase.UserRegistered, NotificationPriorityLevel.High, "critical");
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogProcessingNotificationDelivery should compile")]
    public void Loggers_LogProcessingNotificationDelivery_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationService.Loggers.LogProcessingNotificationDelivery(logger, NotificationUseCase.UserRegistered, "recipient@test.com");
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogHandoffToDelivery should compile")]
    public void Loggers_LogHandoffToDelivery_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationService.Loggers.LogHandoffToDelivery(logger, NotificationUseCase.UserRegistered, "recipient@test.com");
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogBackgroundJobCreated should compile")]
    public void Loggers_LogBackgroundJobCreated_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationService.Loggers.LogBackgroundJobCreated(logger, "job-123", NotificationUseCase.UserRegistered);
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogTemplateNotFound should compile")]
    public void Loggers_LogTemplateNotFound_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationService.Loggers.LogTemplateNotFound(logger, NotificationUseCase.UserRegistered, "UserRegistered");
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogServiceException should compile")]
    public void Loggers_LogServiceException_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationService.Loggers.LogServiceException(logger, new InvalidOperationException("test"), NotificationUseCase.UserRegistered);
        act.Should().NotThrow();
    }
}
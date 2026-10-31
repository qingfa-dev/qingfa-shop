using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Hubs;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationHubLoggerTests
{
    [Fact(DisplayName = "Loggers LogNoActiveProviders should compile")]
    public void Loggers_LogNoActiveProviders_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationHub.Loggers.LogNoActiveProviders(logger, NotificationChannel.Email);
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogAttemptingToSend should compile")]
    public void Loggers_LogAttemptingToSend_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationHub.Loggers.LogAttemptingToSend(logger, "TestProvider", NotificationChannel.Email, 1, "recipient@test.com");
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogSendSuccess should compile")]
    public void Loggers_LogSendSuccess_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationHub.Loggers.LogSendSuccess(logger, "TestProvider", NotificationChannel.Email, "recipient@test.com");
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogProviderFailed should compile")]
    public void Loggers_LogProviderFailed_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationHub.Loggers.LogProviderFailed(logger, "TestProvider", NotificationChannel.Email, "error msg");
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogAllProvidersFailed should compile")]
    public void Loggers_LogAllProvidersFailed_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationHub.Loggers.LogAllProvidersFailed(logger, NotificationChannel.Email, "recipient@test.com");
        act.Should().NotThrow();
    }
}
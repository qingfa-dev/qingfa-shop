using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Application.Common.Integrations.Notifications.Templates;

namespace Infrastructure.Unit.Tests.Integrations.Notifications.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationProviderLoggerTests
{
    [Fact(DisplayName = "Loggers LogSending should compile and accept correct parameters")]
    public void Loggers_LogSending_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationProvider.Loggers.LogSending(logger, "TestProvider", NotificationChannel.Email, NotificationPriorityLevel.Normal, "en", "recipient@test.com");
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogSendFailed should compile and accept correct parameters")]
    public void Loggers_LogSendFailed_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationProvider.Loggers.LogSendFailed(logger, "TestProvider", "error details");
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogSendSuccess should compile and accept correct parameters")]
    public void Loggers_LogSendSuccess_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationProvider.Loggers.LogSendSuccess(logger, "TestProvider", "recipient@test.com");
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Loggers LogSendException should compile and accept correct parameters")]
    public void Loggers_LogSendException_ShouldCompile()
    {
        ILogger logger = NullLogger<object>.Instance;
        Action act = () => NotificationProvider.Loggers.LogSendException(logger, "TestProvider", new InvalidOperationException("test"));
        act.Should().NotThrow();
    }
}
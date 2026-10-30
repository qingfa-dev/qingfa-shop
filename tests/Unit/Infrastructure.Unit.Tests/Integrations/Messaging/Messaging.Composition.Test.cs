using Application.Common.Messaging.Bus;
using Application.Features.Todos.Events;

using Infrastructure.Integrations.Messaging;
using Infrastructure.Integrations.Messaging.Options;
using Infrastructure.Integrations.Messaging.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Unit.Tests.Integrations.Messaging;

/// <summary>Tests for the messaging composition invariants.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging")]
public sealed class MessagingCompositionTest
{
    [Fact(DisplayName = "Disabled messaging should not register the Wolverine bus")]
    public void AddMessaging_WhenDisabled_ShouldKeepTheNoOpPath()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder(new()
        {
            ["Messaging:Enabled"] = "false",
        });

        // Act
        builder.AddMessaging(builder.Configuration);

        // Assert
        builder.Services.Should().NotContain(
            descriptor => descriptor.ServiceType == typeof(IEventBus));
    }

    [Fact(DisplayName = "In-memory messaging should resolve the Wolverine bus")]
    public void AddMessaging_WhenInMemory_ShouldRegisterWolverineBus()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder(new()
        {
            ["Messaging:Enabled"] = "true",
            ["Messaging:Transport"] = "InMemory",
        });

        // Act
        builder.AddMessaging(builder.Configuration);

        // Assert
        builder.Services.Should().Contain(
            descriptor => descriptor.ServiceType == typeof(IEventBus)
                && descriptor.ImplementationType == typeof(WolverineEventBus));
    }

    [Fact(DisplayName = "RabbitMQ without a connection string should fail at composition")]
    public void AddMessaging_WhenRabbitMqHasNoConnection_ShouldThrow()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder(new()
        {
            ["Messaging:Enabled"] = "true",
            ["Messaging:Transport"] = "RabbitMq",
        });

        // Act
        Action act = () => builder.AddMessaging(builder.Configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*connection string*");
    }

    [Fact(DisplayName = "RabbitMQ with a connection string should bind both consumer queues")]
    public void AddMessaging_WhenRabbitMqIsConfigured_ShouldBindQueues()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder(new()
        {
            ["Messaging:Enabled"] = "true",
            ["Messaging:Transport"] = "RabbitMq",
            ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672",
        });

        // Act
        Action act = () => builder.AddMessaging(builder.Configuration);

        // Assert
        act.Should().NotThrow();
        builder.Services.Should().Contain(
            descriptor => descriptor.ServiceType == typeof(IEventBus)
                && descriptor.ImplementationType == typeof(WolverineEventBus));
    }

    [Fact(DisplayName = "Registry should resolve the Todo completed integration event")]
    public void AddMessaging_ShouldRegisterTodoCompletedIntegration()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder(new()
        {
            ["Messaging:Enabled"] = "true",
            ["Messaging:Transport"] = "InMemory",
        });
        builder.AddMessaging(builder.Configuration);
        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        // Act
        Type resolved = provider.GetRequiredService<EventTypeRegistry>()
            .Resolve("todo.completed", 1);

        // Assert
        resolved.Should().Be(typeof(TodoCompletedIntegration));
    }

    [Fact(DisplayName = "Registry should resolve the reminder changed integration event")]
    public void AddMessaging_ShouldRegisterReminderChangedIntegration()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder(new()
        {
            ["Messaging:Enabled"] = "true",
            ["Messaging:Transport"] = "InMemory",
        });
        builder.AddMessaging(builder.Configuration);
        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        // Act
        Type resolved = provider.GetRequiredService<EventTypeRegistry>()
            .Resolve("todo.reminder.changed", 1);

        // Assert
        resolved.Should().Be(typeof(TodoReminderChangedIntegration));
    }

    private static WebApplicationBuilder CreateBuilder(Dictionary<string, string?> values)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(values);
        return builder;
    }
}

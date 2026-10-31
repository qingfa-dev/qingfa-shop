using Application.BuildingBlocks.Messaging.Bus;

using Infrastructure.BuildingBlocks.Adapters.Messaging;
using Infrastructure.BuildingBlocks.Adapters.Messaging.Options;
using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Wolverine;
using Wolverine.RabbitMQ.Internal;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Messaging;

/// <summary>Tests for the messaging composition invariants.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging")]
public sealed class MessagingCompositionTest
{
    [Fact(DisplayName = "Disabled messaging should keep the no-op bus but still register module event types")]
    public void AddMessaging_WhenDisabled_ShouldKeepTheNoOpPath()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder(new()
        {
            ["Messaging:Enabled"] = "false",
        });
        var module = new FakeMessagingModule();

        // Act
        builder.AddMessaging(builder.Configuration, [module]);

        // Assert
        builder.Services.Should().NotContain(
            descriptor => descriptor.ServiceType == typeof(IEventBus));
        module.RegisteredEventTypes.Should().Be(1);
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
        builder.AddMessaging(builder.Configuration, CreateModules());

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
        Action act = () => builder.AddMessaging(builder.Configuration, CreateModules());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*connection string*");
    }

    [Fact(DisplayName = "RabbitMQ with a connection string should compose the Wolverine bus")]
    public void AddMessaging_WhenRabbitMqIsConfigured_ShouldRegisterTheBus()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder(new()
        {
            ["Messaging:Enabled"] = "true",
            ["Messaging:Transport"] = "RabbitMq",
            ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672",
        });

        // Act
        Action act = () => builder.AddMessaging(builder.Configuration, CreateModules());

        // Assert
        act.Should().NotThrow();
        builder.Services.Should().Contain(
            descriptor => descriptor.ServiceType == typeof(IEventBus)
                && descriptor.ImplementationType == typeof(WolverineEventBus));
    }

    private static IMessagingModule[] CreateModules() => [new FakeMessagingModule()];

    private static WebApplicationBuilder CreateBuilder(Dictionary<string, string?> values)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(values);
        return builder;
    }

    /// <summary>No-op module that records the identity-registration hook.</summary>
    private sealed class FakeMessagingModule : IMessagingModule
    {
        public int RegisteredEventTypes { get; private set; }

        /// <inheritdoc />
        public void RegisterEventTypes(EventTypeRegistry eventTypes) => RegisteredEventTypes++;

        /// <inheritdoc />
        public void Configure(
            WolverineOptions options,
            MessagingSetting setting,
            EventTypeRegistry eventTypes,
            RabbitMqTransportExpression? transport)
        {
        }
    }
}

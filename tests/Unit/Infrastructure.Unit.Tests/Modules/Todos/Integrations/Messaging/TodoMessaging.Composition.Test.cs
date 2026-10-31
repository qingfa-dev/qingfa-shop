using Application.Modules.Todos.Events;

using Infrastructure.BuildingBlocks.Adapters.Messaging;
using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;
using Infrastructure.Modules.Todos.Integrations.Messaging;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Unit.Tests.Modules.Todos.Integrations.Messaging;

/// <summary>Tests that the Todo messaging module owns its integration event identities.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Todos")]
[Trait("Feature", "Messaging")]
public sealed class TodoMessagingCompositionTest
{
    [Fact(DisplayName = "Registry should resolve the Todo completed integration event")]
    public void AddMessaging_ShouldRegisterTodoCompletedIntegration()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder(new()
        {
            ["Messaging:Enabled"] = "true",
            ["Messaging:Transport"] = "InMemory",
        });
        builder.AddMessaging(builder.Configuration, CreateModules());

        // Act
        Type resolved = ResolveRegistry(builder).Resolve("todo.completed", 1);

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
        builder.AddMessaging(builder.Configuration, CreateModules());

        // Act
        Type resolved = ResolveRegistry(builder).Resolve("todo.reminder.changed", 1);

        // Assert
        resolved.Should().Be(typeof(TodoReminderChangedIntegration));
    }

    [Fact(DisplayName = "Disabled messaging should still register both Todo integration event identities")]
    public void AddMessaging_WhenDisabled_ShouldStillRegisterTodoIdentities()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder(new()
        {
            ["Messaging:Enabled"] = "false",
        });

        // Act
        builder.AddMessaging(builder.Configuration, CreateModules());
        EventTypeRegistry registry = ResolveRegistry(builder);

        // Assert: the outbox still stages Todo integration rows while messaging is disabled.
        registry.Resolve("todo.completed", 1).Should().Be(typeof(TodoCompletedIntegration));
        registry.Resolve("todo.reminder.changed", 1).Should().Be(typeof(TodoReminderChangedIntegration));
    }

    private static IMessagingModule[] CreateModules() => [new TodoMessagingModule()];

    private static EventTypeRegistry ResolveRegistry(WebApplicationBuilder builder)
    {
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        return provider.GetRequiredService<EventTypeRegistry>();
    }

    private static WebApplicationBuilder CreateBuilder(Dictionary<string, string?> values)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(values);
        return builder;
    }
}

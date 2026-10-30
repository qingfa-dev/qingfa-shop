using System.Text.Json;

using Core.Common.Domain.Events;
using Core.Todos.Categories;
using Core.Todos.Items;
using Core.Todos.Lists;

namespace Core.Unit.Tests.Common.Domain.Events;

/// <summary>Tests for stable event identity and versioning.</summary>
[Trait("Category", "Unit")]
public sealed class DomainEventVersionTest
{
    private sealed record PlainEvent : DomainEvent;

    private sealed record RenamedEvent : DomainEvent
    {
        public override string EventName => "order.placed";
        public override int EventVersion => 3;
    }

    [Fact]
    public void EventName_DefaultsToClrTypeName()
    {
        // Act
        var domainEvent = new PlainEvent();

        // Assert
        domainEvent.EventName.Should().Be(nameof(PlainEvent));
    }

    [Fact]
    public void EventVersion_DefaultsToOne()
    {
        // Act
        var domainEvent = new PlainEvent();

        // Assert
        domainEvent.EventVersion.Should().Be(1);
    }

    [Fact]
    public void EventName_CanBeOverriddenForWireCompatibility()
    {
        // Act
        var domainEvent = new RenamedEvent();

        // Assert
        domainEvent.EventName.Should().Be("order.placed");
        domainEvent.EventVersion.Should().Be(3);
    }

    [Fact]
    public void EventIdAndTimestamp_AreStillPopulated()
    {
        // Act
        var domainEvent = new PlainEvent();

        // Assert
        domainEvent.GetEventId().Should().NotBeNull();
        domainEvent.GetTimestamp().Should().NotBeNull();
    }

    /// <summary>
    /// Freezes the wire names of the production events; renaming a CLR type without a
    /// deliberate version bump is a breaking change.
    /// </summary>
    /// <param name="eventType">The production event type.</param>
    /// <param name="expectedName">The frozen CLR type name.</param>
    [Theory]
    [InlineData(typeof(TodoCreated), "TodoCreated")]
    [InlineData(typeof(TodoUpdated), "TodoUpdated")]
    [InlineData(typeof(TodoStatusChanged), "TodoStatusChanged")]
    [InlineData(typeof(TodoCategoryAdded), "TodoCategoryAdded")]
    [InlineData(typeof(TodoCategoryRemoved), "TodoCategoryRemoved")]
    [InlineData(typeof(TodoListChanged), "TodoListChanged")]
    [InlineData(typeof(TodoDeleted), "TodoDeleted")]
    [InlineData(typeof(TodoRestored), "TodoRestored")]
    [InlineData(typeof(TodoListCreated), "TodoListCreated")]
    [InlineData(typeof(TodoListRenamed), "TodoListRenamed")]
    [InlineData(typeof(TodoCategoryCreated), "TodoCategoryCreated")]
    [InlineData(typeof(TodoCategoryRenamed), "TodoCategoryRenamed")]
    public void ProductionEvents_HaveStableNamesAndInheritTheDefaults(Type eventType, string expectedName)
    {
        // Assert
        eventType.Name.Should().Be(expectedName);
        eventType.GetProperty(nameof(DomainEvent.EventName))!.DeclaringType.Should().Be(typeof(DomainEvent));
        eventType.GetProperty(nameof(DomainEvent.EventVersion))!.DeclaringType.Should().Be(typeof(DomainEvent));
    }

    [Fact]
    public void Serialization_OmitsEventNameAndEventVersion()
    {
        // Arrange
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var domainEvent = new PlainEvent();

        // Act
        string json = JsonSerializer.Serialize(domainEvent, options);

        // Assert: the envelope carries identity and version, not the payload.
        using JsonDocument document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("eventName", out _).Should().BeFalse();
        document.RootElement.TryGetProperty("eventVersion", out _).Should().BeFalse();
    }
}
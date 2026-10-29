using Core.Common.Domain.Events;

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
}
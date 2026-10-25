using Core.Common.Domain.Events;

namespace Core.Tests.Common.Domain.Events;

/// <summary>
/// Tests for the event-scoped metadata extensions
/// (<see cref="DomainEventExtensions"/>).
/// </summary>
[Trait("Category", "Unit")]
public class DomainEventExtensionSpec
{
    private static readonly Guid FixedEventId =
        Guid.Parse("cccccccc-1111-2222-3333-444444444444");

    private static readonly Guid FixedCorrelationId =
        Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");

    private static readonly Guid FixedRequestId =
        Guid.Parse("bbbbbbbb-1111-2222-3333-444444444444");

    private static readonly Guid FixedTenantId =
        Guid.Parse("eeeeeeee-1111-2222-3333-444444444444");

    private static readonly DateTimeOffset FixedOccurredOnUtc =
        new(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);

    private sealed record TestEvent : DomainEvent;

    [Fact]
    public void WellKnownAccessors_ShouldRoundtripThroughEventExtensions()
    {
        // Arrange
        IDomainEvent evt = new TestEvent();

        // Act
        evt.SetEventId(FixedEventId);
        evt.SetCorrelationId(FixedCorrelationId);
        evt.SetTenantId(FixedTenantId);
        evt.SetTimestamp(FixedOccurredOnUtc);

        // Assert
        evt.GetEventId().Should().Be(FixedEventId);
        evt.GetCorrelationId().Should().Be(FixedCorrelationId);
        evt.GetTenantId().Should().Be(FixedTenantId);
        evt.GetTimestamp().Should().Be(FixedOccurredOnUtc);
    }

    [Fact]
    public void MergeMetadata_ThroughEventReceiver_ShouldOverwriteAndAdd()
    {
        // Arrange
        var evt = new TestEvent();
        evt.SetCorrelationId(FixedCorrelationId);
        IMetadata metadata = DefaultMetadata.Create()
            .With(MetadataConstant.Keyword.CorrelationId, FixedRequestId)
            .With(MetadataConstant.Keyword.TenantId, FixedTenantId);

        // Act
        var merged = evt.MergeMetadata(metadata);

        // Assert
        merged.Should().BeSameAs(evt.Metadata);
        evt.GetCorrelationId().Should().Be(FixedRequestId);
        evt.GetTenantId().Should().Be(FixedTenantId);
    }

    [Fact]
    public void EventExtensions_WhenEventIsNull_ShouldThrow()
    {
        // Arrange
        IDomainEvent evt = null!;

        // Act
        Action getEvent = () => evt.GetEventId();
        Action setEventId = () => evt.SetEventId(FixedEventId);

        // Assert
        getEvent.Should().Throw<ArgumentNullException>();
        setEventId.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DomainEventExtensions_ShouldBeStaticClass()
    {
        typeof(DomainEventExtensions).IsAbstract.Should().BeTrue();
        typeof(DomainEventExtensions).IsSealed.Should().BeTrue();
    }
}
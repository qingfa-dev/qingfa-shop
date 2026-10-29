using Core.Common.Concerns.Foundation.Versionable;
using Core.Common.Domain.Aggregates;
using Core.Common.Domain.Events;

namespace Core.Unit.Tests.Common.Domain.Aggregates;

/// <summary>
/// Tests for <see cref="Aggregate{TKey, TEvent}"/> event collection and
/// metadata envelope.
/// </summary>
[Trait("Category", "Unit")]
public class AggregateSpec
{
    private static readonly Guid FixedCorrelationId =
        Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");

    private sealed record TestEvent : DomainEvent;

    private sealed class TestAggregate : Aggregate<Guid, TestEvent>
    {
        public TestAggregate()
        {
        }

        public TestAggregate(IMetadata? metadata)
            : base(metadata)
        {
        }

        public void Raise(TestEvent e) => AddEvent(e);
    }

    [Fact]
    public void Events_ShouldBeEmptyOnCreation()
    {
        // Arrange
        // Act
        var aggregate = new TestAggregate();

        // Assert
        aggregate.Events.Should().BeEmpty();
    }

    [Fact]
    public void AddEvent_ShouldAppendInOrder()
    {
        // Arrange
        var aggregate = new TestAggregate();
        var first = new TestEvent();
        var second = new TestEvent();

        // Act
        aggregate.Raise(first);
        aggregate.Raise(second);

        // Assert
        aggregate.Events.Should().HaveCount(2);
        aggregate.Events[0].Should().BeSameAs(first);
        aggregate.Events[1].Should().BeSameAs(second);
    }

    [Fact]
    public void RemoveAllEvents_ShouldClearEvents()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.Raise(new TestEvent());

        // Act
        aggregate.RemoveAllEvents();

        // Assert
        aggregate.Events.Should().BeEmpty();
    }

    [Fact]
    public void Metadata_ShouldBeNonNullOrEmptyByDefault()
    {
        // Arrange
        // Act
        var aggregate = new TestAggregate();

        // Assert
        aggregate.Metadata.Should().NotBeNull();
        aggregate.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void MetadataConstructor_ShouldMergeProvidedBag()
    {
        // Arrange
        IMetadata metadata = DefaultMetadata.Create()
            .With(MetadataConstant.Keyword.CorrelationId, FixedCorrelationId);

        // Act
        var aggregate = new TestAggregate(metadata);

        // Assert
        aggregate.GetCorrelationId().Should().Be(FixedCorrelationId);
    }

    [Fact]
    public void Aggregate_ShouldImplementAggregateContracts()
    {
        // Arrange
        IAggregate<TestEvent> withEvents = new TestAggregate();
        IAggregate marker = withEvents;

        // Act
        var events = withEvents.Events;

        // Assert
        events.Should().BeEmpty();
        marker.Metadata.Should().NotBeNull();
        withEvents.Should().BeAssignableTo<IHasMetadata>();
    }

    [Fact]
    public void Version_ShouldStartAtZeroAndBeWritable()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        long initial = aggregate.Version;
        aggregate.Version = 4;

        // Assert
        initial.Should().Be(0);
        aggregate.Version.Should().Be(4);
    }

    [Fact]
    public void Aggregate_ShouldImplementVersionable()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        IVersionable versionable = aggregate;

        // Assert
        versionable.Version.Should().Be(0);
    }
}

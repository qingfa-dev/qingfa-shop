using Core.Common.Domain.Aggregates;
using Core.Common.Domain.Entities;
using Core.Common.Domain.Events;

namespace Core.Tests.Common.Domain.Aggregates;

/// <summary>
/// Tests for <see cref="AggregateRoot{TKey}"/> identity, event collection,
/// and metadata envelope.
/// </summary>
[Trait("Category", "Unit")]
public class AggregateRootSpec
{
    private static readonly Guid FixedCorrelationId =
        Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");

    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
        public TestAggregateRoot()
        {
        }

        public TestAggregateRoot(IMetadata? metadata)
            : base(metadata)
        {
        }

        public TestAggregateRoot(Guid id)
            : base(id)
        {
        }

        public void Raise(IDomainEvent e) => AddEvent(e);
    }

    [Fact]
    public void ParameterlessConstructor_ShouldCreateTransientRoot()
    {
        // Arrange
        // Act
        var root = new TestAggregateRoot();

        // Assert
        root.Id.Should().Be(Guid.Empty);
        root.IsTransient.Should().BeTrue();
        root.Events.Should().BeEmpty();
    }

    [Fact]
    public void IdConstructor_ShouldSetIdAndBePersistent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var root = new TestAggregateRoot(id);

        // Assert
        root.Id.Should().Be(id);
        root.IsTransient.Should().BeFalse();
    }

    [Fact]
    public void MetadataConstructor_ShouldMergeProvidedBag()
    {
        // Arrange
        IMetadata metadata = DefaultMetadata.Create()
            .With(MetadataConstant.Keyword.CorrelationId, FixedCorrelationId);

        // Act
        var root = new TestAggregateRoot(metadata);

        // Assert
        root.GetCorrelationId().Should().Be(FixedCorrelationId);
    }

    [Fact]
    public void AddEventThenRemoveAll_ShouldAppendThenClear()
    {
        // Arrange
        var root = new TestAggregateRoot();
        IDomainEvent evt = new DomainEventStub();

        // Act
        root.Raise(evt);
        root.RemoveAllEvents();

        // Assert
        root.Events.Should().BeEmpty();
    }

    [Fact]
    public void AggregateRoot_ShouldWorkThroughAllContractViews()
    {
        // Arrange
        var id = Guid.NewGuid();
        IAggregateRoot<IDomainEvent, Guid> root =
            new TestAggregateRoot(id);

        // Act
        IEntity<Guid> entity = root;
        IAggregate<IDomainEvent> aggregate = root;

        // Assert
        entity.Id.Should().Be(id);
        aggregate.Events.Should().BeEmpty();
        root.Should().BeAssignableTo<IHasMetadata>();
    }

    private sealed record DomainEventStub : DomainEvent;
}
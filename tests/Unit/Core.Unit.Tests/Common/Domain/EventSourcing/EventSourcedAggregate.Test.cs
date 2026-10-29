using Core.Common.Domain.Events;
using Core.Common.Domain.EventSourcing;

namespace Core.Unit.Tests.Common.Domain.EventSourcing;

/// <summary>Tests for the event-sourced aggregate state machine.</summary>
[Trait("Category", "Unit")]
public sealed class EventSourcedAggregateTest
{
    private sealed record Created(Guid Id, string Name) : DomainEvent;

    private sealed record Renamed(string Name) : DomainEvent;

    private sealed record Unknown(string Value) : DomainEvent;

    private sealed class TestAggregate : EventSourcedAggregate<Guid>
    {
        public string Name { get; private set; } = string.Empty;

        public static TestAggregate Create(Guid id, string name)
        {
            var aggregate = new TestAggregate();
            aggregate.Raise(new Created(id, name));
            return aggregate;
        }

        public void Rename(string name) => Raise(new Renamed(name));

        protected override void When(IDomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case Created created:
                    Id = created.Id;
                    Name = created.Name;
                    break;

                case Renamed renamed:
                    Name = renamed.Name;
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Event '{domainEvent.GetType().Name}' is not supported.");
            }
        }
    }

    private sealed class TestAggregateFactory : IAggregateFactory<TestAggregate>
    {
        public TestAggregate Create() => new();
    }

    [Fact]
    public void Raise_AppliesEventAndIncrementsVersion()
    {
        // Act
        TestAggregate aggregate = TestAggregate.Create(Guid.NewGuid(), "first");

        // Assert
        aggregate.Version.Should().Be(1);
        aggregate.Name.Should().Be("first");
        aggregate.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<Created>();
    }

    [Fact]
    public void LoadFromHistory_AppliesEventsWithoutMarkingThemUncommitted()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        var history = new IDomainEvent[]
        {
            new Created(id, "first"),
            new Renamed("second"),
        };
        var aggregate = new TestAggregate();

        // Act
        aggregate.LoadFromHistory(2, history);

        // Assert
        aggregate.Id.Should().Be(id);
        aggregate.Name.Should().Be("second");
        aggregate.Version.Should().Be(2);
        aggregate.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearUncommittedEvents_RemovesOnlyUncommittedEvents()
    {
        // Arrange
        TestAggregate aggregate = TestAggregate.Create(Guid.NewGuid(), "first");

        // Act
        aggregate.ClearUncommittedEvents();

        // Assert
        aggregate.UncommittedEvents.Should().BeEmpty();
        aggregate.Version.Should().Be(1);
        aggregate.Name.Should().Be("first");
    }

    [Fact]
    public void When_WithUnknownEvent_Throws()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        Action act = () => aggregate.LoadFromHistory(1, [new Unknown("x")]);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown*");
    }

    [Fact]
    public void Factory_CreatesAnEmptyAggregate()
    {
        // Arrange
        IAggregateFactory<TestAggregate> factory = new TestAggregateFactory();

        // Act
        TestAggregate aggregate = factory.Create();

        // Assert
        aggregate.Version.Should().Be(0);
        aggregate.UncommittedEvents.Should().BeEmpty();
    }
}
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

        public void RestoreVersionFromSnapshot(long version) => RestoreVersion(version);

        public void Break() => Raise(new Unknown("x"));

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
    public void TryLoadFromHistory_WithValidHistory_ShouldReplay()
    {
        // Arrange
        var history = new IDomainEvent[]
        {
            new Created(Guid.NewGuid(), "first"),
            new Renamed("second"),
        };
        var aggregate = new TestAggregate();

        // Act
        Result result = aggregate.TryLoadFromHistory(2, history);

        // Assert
        result.IsSuccess.Should().BeTrue();
        aggregate.Version.Should().Be(2);
        aggregate.Name.Should().Be("second");
    }

    [Fact]
    public void TryLoadFromHistory_WhenCalledTwice_ShouldReturnAlreadyLoaded()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.TryLoadFromHistory(0, []);

        // Act
        Result result = aggregate.TryLoadFromHistory(0, []);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(EventReplayOutcome.Codes.AlreadyLoaded);
    }

    [Fact]
    public void TryLoadFromHistory_WhenVersionIsBelowEventCount_ShouldReturnVersionInvalid()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        Result result = aggregate.TryLoadFromHistory(
            1,
            [new Created(Guid.NewGuid(), "first"), new Renamed("second")]);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(EventReplayOutcome.Codes.VersionInvalid);
    }

    [Fact]
    public void TryLoadFromHistory_WhenApplyFails_ShouldReturnApplyFailed()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        Result result = aggregate.TryLoadFromHistory(1, [new Unknown("x")]);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(EventReplayOutcome.Codes.ApplyFailed);
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
    public void RestoreVersion_SetsTheVersionWithoutMarkingHistoryLoaded()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        aggregate.RestoreVersionFromSnapshot(7);

        // Assert: the snapshot position survives, and replay can still continue after it.
        aggregate.Version.Should().Be(7);

        Result replayed = aggregate.TryLoadFromHistory(8, [new Renamed("later")]);

        replayed.IsSuccess.Should().BeTrue();
        aggregate.Version.Should().Be(8);
        aggregate.Name.Should().Be("later");
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

    [Fact]
    public void Raise_WhenHandlerThrows_DoesNotAdvanceVersionOrBufferEvent()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        Action act = () => aggregate.Break();

        // Assert
        act.Should().Throw<InvalidOperationException>();
        aggregate.Version.Should().Be(0);
        aggregate.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void LoadFromHistory_WithEmptyHistory_KeepsVersion()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        aggregate.LoadFromHistory(0, []);

        // Assert
        aggregate.Version.Should().Be(0);
        aggregate.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void LoadFromHistory_WithNullHistory_Throws()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        Action act = () => aggregate.LoadFromHistory(0, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LoadFromHistory_WithNegativeVersion_Throws()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        Action act = () => aggregate.LoadFromHistory(-1, []);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LoadFromHistory_WhenCalledTwice_Throws()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.LoadFromHistory(1, [new Created(Guid.NewGuid(), "first")]);

        // Act
        Action act = () => aggregate.LoadFromHistory(1, [new Created(Guid.NewGuid(), "second")]);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*once*");
    }

    [Fact]
    public void LoadFromHistory_WhenAggregateHasUncommittedEvents_Throws()
    {
        // Arrange
        TestAggregate aggregate = TestAggregate.Create(Guid.NewGuid(), "first");

        // Act
        Action act = () => aggregate.LoadFromHistory(1, [new Created(Guid.NewGuid(), "second")]);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LoadFromHistory_WhenVersionBelowEventCount_Throws()
    {
        // Arrange
        var aggregate = new TestAggregate();
        var history = new IDomainEvent[]
        {
            new Created(Guid.NewGuid(), "first"),
            new Renamed("second"),
        };

        // Act
        Action act = () => aggregate.LoadFromHistory(1, history);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Raise_AfterLoadFromHistory_ContinuesFromStreamVersion()
    {
        // Arrange
        var aggregate = new TestAggregate();
        var history = new IDomainEvent[]
        {
            new Created(Guid.NewGuid(), "first"),
            new Renamed("second"),
        };
        aggregate.LoadFromHistory(2, history);

        // Act
        aggregate.Rename("third");

        // Assert
        aggregate.Version.Should().Be(3);
        aggregate.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<Renamed>()
            .Which.Name.Should().Be("third");
    }
}
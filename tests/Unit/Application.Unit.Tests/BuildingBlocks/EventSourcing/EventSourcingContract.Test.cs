using Application.BuildingBlocks.EventSourcing;

namespace Application.Unit.Tests.BuildingBlocks.EventSourcing;

/// <summary>Tests for the event-sourcing contracts.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "EventSourcing")]
public sealed class EventSourcingContractTest
{
    private sealed class TodoReminder { }
    private sealed class Todo { }

    [Fact]
    public void EventStreamName_ShouldKebabCaseTheAggregateAndAppendTheId()
    {
        Guid id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        EventStreamName.For<TodoReminder>(id).Should().Be($"todo-reminder:{id:D}");
        EventStreamName.For<Todo>(id).Should().Be($"todo:{id:D}");
    }

    [Fact]
    public void IntervalSnapshotPolicy_ShouldTriggerAtTheBoundary()
    {
        var policy = new IntervalSnapshotPolicy(3);

        policy.ShouldSnapshot(10, 2).Should().BeFalse();
        policy.ShouldSnapshot(10, 3).Should().BeTrue();
        policy.ShouldSnapshot(10, 4).Should().BeTrue();
    }

    [Fact]
    public void IntervalSnapshotPolicy_WithNonPositiveInterval_ShouldThrowAtCompositionBoundary()
    {
        Action act = () => _ = new IntervalSnapshotPolicy(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Outcomes_ShouldExposeStableCodes()
    {
        EventSourcingOutcome.Codes.AppendConflict.Should().Be("EventStore.Append.Conflict");
        EventSourcingOutcome.Codes.ReadFailed.Should().Be("EventStore.Read.Failed");
        EventSourcingOutcome.Codes.SnapshotReadFailed.Should().Be("Snapshot.Read.Failed");
        EventSourcingOutcome.Codes.ProjectionApplyFailed.Should().Be("Projection.Apply.Failed");
        EventSourcingOutcome.Codes.StreamNotFound.Should().Be("Repository.Stream.NotFound");
    }

    [Fact]
    public void Outcomes_ShouldBuildActionableErrors()
    {
        Result result = Result.Fail(
            EventSourcingOutcome.Failure.AppendConflict("todo:1", 2));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(EventSourcingOutcome.Codes.AppendConflict);
    }
}

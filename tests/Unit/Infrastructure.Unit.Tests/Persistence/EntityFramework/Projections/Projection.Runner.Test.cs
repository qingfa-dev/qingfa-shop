using Application.Common.EventSourcing;

using Infrastructure.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.Persistence.EntityFramework.Projections;

using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Projections;

/// <summary>Tests for projection checkpoint progression.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "EventSourcing")]
public sealed class ProjectionRunnerTest
{
    [Fact(DisplayName = "Runner should apply events and advance the checkpoint")]
    public async Task RunOnce_WhenApplySucceeds_ShouldAdvance()
    {
        var projection = new FakeProjection();
        var checkpoints = new FakeCheckpointStore();
        var store = new FakeEventStore([CreateEvent("stream", 1, 1), CreateEvent("stream", 2, 2)]);
        var runner = new ProjectionRunner(
            store, [projection], checkpoints, new EventStoreSetting(),
            NullLogger<ProjectionRunner>.Instance);

        Result result = await runner.RunOnceAsync();

        result.IsSuccess.Should().BeTrue();
        projection.Applied.Should().HaveCount(2);
        checkpoints.Position.Should().Be(2);
    }

    [Fact(DisplayName = "Runner should stop at a failing event and keep the checkpoint")]
    public async Task RunOnce_WhenApplyFails_ShouldNotAdvance()
    {
        var projection = new FakeProjection { ShouldFail = true };
        var checkpoints = new FakeCheckpointStore();
        var store = new FakeEventStore([CreateEvent("stream", 1, 1)]);
        var runner = new ProjectionRunner(
            store, [projection], checkpoints, new EventStoreSetting(),
            NullLogger<ProjectionRunner>.Instance);

        Result result = await runner.RunOnceAsync();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(EventSourcingOutcome.Codes.ProjectionApplyFailed);
        checkpoints.Position.Should().Be(0);
    }

    private static StoredEvent CreateEvent(string streamId, long version, long position)
        => new(streamId, version, Guid.NewGuid(), "test", 1, "{}", [], DateTimeOffset.UtcNow, position);

    private sealed class FakeEventStore(IReadOnlyList<StoredEvent> events) : IEventStore
    {
        public Task<Result<long>> AppendAsync(string streamId, long expectedVersion, IReadOnlyCollection<Core.Common.Domain.Events.IDomainEvent> events, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<long>.Success(expectedVersion));

        public Task<Result<IReadOnlyList<StoredEvent>>> ReadStreamAsync(string streamId, long fromVersion, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<IReadOnlyList<StoredEvent>>.Success([]));

        public Task<Result<IReadOnlyList<StoredEvent>>> ReadAllAsync(long fromPosition, int batchSize, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<IReadOnlyList<StoredEvent>>.Success(
                [.. events.Where(stored => stored.Position >= fromPosition).Take(batchSize)]));
    }

    private sealed class FakeProjection : IProjection
    {
        public string Name => "fake";
        public bool ShouldFail { get; init; }
        public List<StoredEvent> Applied { get; } = [];

        public Task<Result> ApplyAsync(StoredEvent storedEvent, CancellationToken cancellationToken = default)
        {
            if (ShouldFail)
                return Task.FromResult(Result.Fail(Error.InternalServerError("apply", "failed")));

            Applied.Add(storedEvent);
            return Task.FromResult(Result.Ok());
        }
    }

    private sealed class FakeCheckpointStore : IProjectionCheckpointStore
    {
        public long Position { get; private set; }

        public Task<Result<long>> GetPositionAsync(string projection, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<long>.Success(Position));

        public Task<Result> SavePositionAsync(string projection, long position, CancellationToken cancellationToken = default)
        {
            Position = position;
            return Task.FromResult(Result.Ok());
        }
    }
}

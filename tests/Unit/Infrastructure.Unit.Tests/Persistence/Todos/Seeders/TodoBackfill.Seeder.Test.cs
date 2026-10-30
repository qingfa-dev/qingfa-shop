using System.Globalization;
using System.Text.Json;

using Application.Common.EventSourcing;
using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Seeders;
using Application.Features.Todos.Items.Models;

using Core.Common.Domain.Events;
using Core.Common.Monad.Metadata;
using Core.Todos.Categories;
using Core.Todos.Items;
using Core.Todos.Lists;

using Infrastructure.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.Persistence.Schemas.Todos.EntityConfiguration;
using Infrastructure.Persistence.Schemas.Todos.Seeders;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Unit.Tests.Persistence.Todos.Seeders;

/// <summary>Unit tests for <see cref="TodoBackfillSeeder"/>.</summary>
public sealed class TodoBackfillSeederTest : IAsyncLifetime
{
    private readonly DateTimeOffset _now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    private SqliteConnection _connection = null!;
    private BackfillTestContext _context = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BackfillTestContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BackfillTestContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact(DisplayName = "Backfill should expose stable seeder metadata")]
    public void Metadata_ShouldDescribeTheBackfillStep()
    {
        // Arrange
        TodoBackfillSeeder seeder = CreateSeeder();

        // Assert
        seeder.Name.Should().Be("seeder:todo-backfill");
        seeder.DisplayName.Should().Be("Backfill Todo event streams");
        seeder.Order.Should().Be(35);
        seeder.DependsOn.Should().Equal("seeder:todos");
        seeder.IsRequired.Should().BeFalse();
        ((IPersistenceSeeder)seeder).EntityType.Should().Be(typeof(TodoReadModel));
        ((IPersistenceSeeder)seeder).KeyType.Should().Be(typeof(Guid));
    }

    [Fact(DisplayName = "Backfill should do nothing when the setting is disabled")]
    public async Task ExecuteAsync_WhenDisabled_ShouldNotAppend()
    {
        // Arrange
        TodoReadModel row = await SeedRowAsync(version: 42);
        var eventStore = new InMemoryEventStore();
        var snapshots = new RecordingSnapshotStore();
        var checkpoints = new RecordingCheckpointStore();
        TodoBackfillSeeder seeder = CreateSeeder(
            enabled: false, eventStore, snapshots, checkpoints);

        // Act
        Result result = await seeder.ExecuteAsync(CreateInitializationContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        eventStore.AppendCalls.Should().Be(0);
        snapshots.Saved.Should().BeEmpty();
        checkpoints.Positions.Should().BeEmpty();
        (await ReadVersionAsync(row.Id)).Should().Be(42);
    }

    [Fact(DisplayName = "Backfill should append the full history and reset the legacy version")]
    public async Task ExecuteAsync_WhenStreamIsMissing_ShouldBackfillAndResetVersion()
    {
        // Arrange
        Guid categoryOne = Guid.NewGuid();
        Guid categoryTwo = Guid.NewGuid();
        Guid listId = Guid.NewGuid();
        TodoReadModel row = await SeedRowAsync(row =>
        {
            row.Title = "Ship the migration";
            row.Description = "Move todos to the event store";
            row.Priority = TodoPriority.High;
            row.DueAtUtc = _now.AddDays(2);
            row.ListId = listId;
            row.CategoryIds = [categoryOne, categoryTwo];
            row.Status = TodoStatus.Completed;
            row.CompletedAtUtc = _now.AddHours(2);
            row.ModifiedAtUtc = _now.AddHours(1);
            row.ModifiedBy = "editor";
            row.IsDeleted = true;
            row.DeletedAtUtc = _now.AddHours(3);
            row.DeletedBy = "deleter";
        }, version: 42);

        var eventStore = new InMemoryEventStore();
        long versionObservedBeforeAppend = -1;
        eventStore.BeforeAppend = async () =>
            versionObservedBeforeAppend = await ReadVersionAsync(row.Id);
        var snapshots = new RecordingSnapshotStore();
        var checkpoints = new RecordingCheckpointStore();
        TodoBackfillSeeder seeder = CreateSeeder(eventStore: eventStore, snapshots: snapshots, checkpoints: checkpoints);

        // Act
        Result result = await seeder.ExecuteAsync(CreateInitializationContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        eventStore.AppendCalls.Should().Be(1);
        eventStore.ExpectedVersions.Should().Equal(0L);

        // Assert: the reset is visible before the appended events can be projected.
        versionObservedBeforeAppend.Should().Be(0);

        string streamId = eventStore.SingleStreamId();
        streamId.Should().Be(EventStreamName.For<Todo>(row.Id));

        List<IDomainEvent> events = eventStore.Events(streamId);
        events.Select(domainEvent => domainEvent.GetType()).Should().Equal(
            typeof(TodoCreated),
            typeof(TodoCategoryAdded),
            typeof(TodoCategoryAdded),
            typeof(TodoUpdated),
            typeof(TodoStatusChanged),
            typeof(TodoDeleted),
            typeof(TodoUpdated));

        var created = (TodoCreated)events[0];
        created.TodoId.Should().Be(row.Id);
        created.TenantId.Should().Be(row.TenantId);
        created.Title.Should().Be("Ship the migration");
        created.Priority.Should().Be(TodoPriority.High);

        var added = (TodoCategoryAdded)events[1];
        added.CategoryId.Should().Be(categoryOne);
        var secondAdded = (TodoCategoryAdded)events[2];
        secondAdded.CategoryId.Should().Be(categoryTwo);

        var updated = (TodoUpdated)events[3];
        updated.ModifiedAtUtc.Should().Be(_now.AddHours(1));
        updated.ModifiedBy.Should().Be("editor");

        var status = (TodoStatusChanged)events[4];
        status.TenantId.Should().Be(row.TenantId);
        status.From.Should().Be(TodoStatus.Pending);
        status.To.Should().Be(TodoStatus.Completed);
        status.OccurredAtUtc.Should().Be(_now.AddHours(2));

        var deleted = (TodoDeleted)events[5];
        deleted.IsHard.Should().BeFalse();
        deleted.OccurredAtUtc.Should().Be(_now.AddHours(3));
        deleted.Actor.Should().Be("deleter");

        // Assert: the trailing update restores the legacy modified stamp that the status
        // event would otherwise overwrite with CompletedAtUtc.
        var restoredAudit = (TodoUpdated)events[6];
        restoredAudit.ModifiedAtUtc.Should().Be(_now.AddHours(1));
        restoredAudit.ModifiedBy.Should().Be("editor");

        // Assert: the legacy aggregate version is zeroed so the projection applies the replay.
        (await ReadVersionAsync(row.Id)).Should().Be(0);

        Snapshot snapshot = snapshots.Saved.Should().ContainSingle().Subject;
        snapshot.StreamId.Should().Be(streamId);
        snapshot.Version.Should().Be(7);
        snapshot.TakenOnUtc.Should().Be(_now);

        TodoSnapshot state = JsonSerializer.Deserialize<TodoSnapshot>(snapshot.State)!;
        state.Id.Should().Be(row.Id);
        state.Status.Should().Be(TodoStatus.Completed);
        state.Priority.Should().Be(TodoPriority.High);
        state.CategoryIds.Should().Equal(categoryOne, categoryTwo);
        state.IsDeleted.Should().BeTrue();
        state.IsHardDeleted.Should().BeFalse();
        state.ModifiedAtUtc.Should().Be(_now.AddHours(1));
        state.ModifiedBy.Should().Be("editor");
        state.Version.Should().Be(7);

        checkpoints.Positions.Should().ContainKey($"TodoBackfill:{row.Id}")
            .WhoseValue.Should().Be(7);
    }

    [Fact(DisplayName = "Backfill should not append a trailing update when the status stamp matches")]
    public async Task ExecuteAsync_WhenStatusStampMatchesModified_ShouldNotAppendATrailingUpdate()
    {
        // Arrange: the status event writes the same modified stamp, so no repair is needed.
        TodoReadModel row = await SeedRowAsync(row =>
        {
            row.Status = TodoStatus.Completed;
            row.CompletedAtUtc = _now.AddHours(1);
            row.ModifiedAtUtc = _now.AddHours(1);
            row.ModifiedBy = "editor";
        });
        var eventStore = new InMemoryEventStore();
        TodoBackfillSeeder seeder = CreateSeeder(eventStore: eventStore);

        // Act
        Result result = await seeder.ExecuteAsync(CreateInitializationContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        List<IDomainEvent> events = eventStore.Events(EventStreamName.For<Todo>(row.Id));
        events.Select(domainEvent => domainEvent.GetType()).Should().Equal(
            typeof(TodoCreated),
            typeof(TodoUpdated),
            typeof(TodoStatusChanged));
    }

    [Fact(DisplayName = "Backfill should skip a row whose stream and marker already exist")]
    public async Task ExecuteAsync_WhenStreamAndMarkerExist_ShouldSkipTheRow()
    {
        // Arrange
        TodoReadModel row = await SeedRowAsync(version: 42);
        var eventStore = new InMemoryEventStore();
        string streamId = EventStreamName.For<Todo>(row.Id);
        await eventStore.AppendAsync(
            streamId,
            0,
            [new TodoCreated(row.Id, row.TenantId, row.Title, null, row.Priority, null, null, row.CreatedAtUtc, row.CreatedBy)]);

        var snapshots = new RecordingSnapshotStore();
        var checkpoints = new RecordingCheckpointStore();
        checkpoints.Positions[$"TodoBackfill:{row.Id}"] = 1;
        TodoBackfillSeeder seeder = CreateSeeder(eventStore: eventStore, snapshots: snapshots, checkpoints: checkpoints);

        // Act
        Result result = await seeder.ExecuteAsync(CreateInitializationContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        eventStore.AppendCalls.Should().Be(1);
        eventStore.Events(streamId).Should().HaveCount(1);
        snapshots.Saved.Should().BeEmpty();
        checkpoints.Positions[$"TodoBackfill:{row.Id}"].Should().Be(1);
        (await ReadVersionAsync(row.Id)).Should().Be(42);
    }

    [Fact(DisplayName = "Backfill should recover only the marker without overwriting the snapshot")]
    public async Task ExecuteAsync_WhenMarkerIsMissing_ShouldRecoverOnlyTheMarker()
    {
        // Arrange: the stream exists from a crashed run, but the marker is still zero.
        TodoReadModel row = await SeedRowAsync(version: 42);
        var eventStore = new InMemoryEventStore();
        string streamId = EventStreamName.For<Todo>(row.Id);
        await eventStore.AppendAsync(
            streamId,
            0,
            [new TodoCreated(row.Id, row.TenantId, row.Title, null, row.Priority, null, null, row.CreatedAtUtc, row.CreatedBy)]);

        var snapshots = new Mock<ISnapshotStore>();
        var checkpoints = new RecordingCheckpointStore();
        TodoBackfillSeeder seeder = CreateSeeder(
            eventStore: eventStore, snapshots: snapshots.Object, checkpoints: checkpoints);

        // Act
        Result first = await seeder.ExecuteAsync(CreateInitializationContext());
        Result second = await seeder.ExecuteAsync(CreateInitializationContext());

        // Assert
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        eventStore.AppendCalls.Should().Be(1);
        eventStore.Events(streamId).Should().HaveCount(1);

        // Verify: recovery must not write a snapshot from the lagging read model.
        snapshots.Verify(
            store => store.SaveAsync(It.IsAny<Snapshot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        checkpoints.SaveCalls.Should().Be(1);
        checkpoints.Positions.Should().ContainKey($"TodoBackfill:{row.Id}")
            .WhoseValue.Should().Be(1);
        (await ReadVersionAsync(row.Id)).Should().Be(42);
    }

    [Fact(DisplayName = "Backfill should forward the cancellation token while recovering a marker")]
    public async Task ExecuteAsync_WhenMarkerIsMissing_ShouldForwardTokenToRecovery()
    {
        // Arrange
        TodoReadModel row = await SeedRowAsync(version: 42);
        var eventStore = new InMemoryEventStore();
        string streamId = EventStreamName.For<Todo>(row.Id);
        await eventStore.AppendAsync(
            streamId,
            0,
            [new TodoCreated(row.Id, row.TenantId, row.Title, null, row.Priority, null, null, row.CreatedAtUtc, row.CreatedBy)]);

        using var cancellation = new CancellationTokenSource();
        string marker = $"TodoBackfill:{row.Id}";
        var snapshots = new Mock<ISnapshotStore>();
        var checkpoints = new Mock<IProjectionCheckpointStore>();
        checkpoints
            .Setup(store => store.GetPositionAsync(marker, cancellation.Token))
            .ReturnsAsync(Result<long>.Success(0));
        checkpoints
            .Setup(store => store.SavePositionAsync(marker, 1, cancellation.Token))
            .ReturnsAsync(Result.Ok());

        TodoBackfillSeeder seeder = CreateSeeder(
            eventStore: eventStore, snapshots: snapshots.Object, checkpoints: checkpoints.Object);

        // Act
        Result result = await seeder.ExecuteAsync(
            CreateInitializationContext(), cancellation.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        checkpoints.Verify(
            store => store.GetPositionAsync(marker, cancellation.Token), Times.Once);
        checkpoints.Verify(
            store => store.SavePositionAsync(marker, 1, cancellation.Token), Times.Once);
        snapshots.Verify(
            store => store.SaveAsync(It.IsAny<Snapshot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Backfill should be idempotent when it runs twice")]
    public async Task ExecuteAsync_WhenRunTwice_ShouldNotDuplicateEvents()
    {
        // Arrange
        TodoReadModel row = await SeedRowAsync();
        var eventStore = new InMemoryEventStore();
        var snapshots = new RecordingSnapshotStore();
        var checkpoints = new RecordingCheckpointStore();
        TodoBackfillSeeder seeder = CreateSeeder(eventStore: eventStore, snapshots: snapshots, checkpoints: checkpoints);

        // Act
        Result first = await seeder.ExecuteAsync(CreateInitializationContext());
        Result second = await seeder.ExecuteAsync(CreateInitializationContext());

        // Assert
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        eventStore.AppendCalls.Should().Be(1);
        eventStore.Events(EventStreamName.For<Todo>(row.Id)).Should().ContainSingle();
        snapshots.Saved.Should().ContainSingle();
        checkpoints.Positions.Should().ContainSingle();
        (await ReadVersionAsync(row.Id)).Should().Be(0);
    }

    [Fact(DisplayName = "Backfill should honour the batch size and make progress across runs")]
    public async Task ExecuteAsync_WithBatchSizeOne_ShouldBackfillOneRowPerRun()
    {
        // Arrange
        TodoReadModel older = await SeedRowAsync(row => row.CreatedAtUtc = _now.AddHours(-2));
        TodoReadModel newer = await SeedRowAsync(row => row.CreatedAtUtc = _now.AddHours(-1));
        var eventStore = new InMemoryEventStore();
        TodoBackfillSeeder seeder = CreateSeeder(eventStore: eventStore, batchSize: 1);

        // Act
        Result first = await seeder.ExecuteAsync(CreateInitializationContext());
        Result second = await seeder.ExecuteAsync(CreateInitializationContext());

        // Assert
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        eventStore.AppendCalls.Should().Be(2);
        eventStore.Events(EventStreamName.For<Todo>(older.Id)).Should().ContainSingle();
        eventStore.Events(EventStreamName.For<Todo>(newer.Id)).Should().ContainSingle();
    }

    [Fact(DisplayName = "Backfill should clamp a non-positive batch size to one row")]
    public async Task ExecuteAsync_WithNonPositiveBatchSize_ShouldStillBackfill()
    {
        // Arrange
        await SeedRowAsync();
        await SeedRowAsync();
        var eventStore = new InMemoryEventStore();
        TodoBackfillSeeder seeder = CreateSeeder(eventStore: eventStore, batchSize: 0);

        // Act
        Result result = await seeder.ExecuteAsync(CreateInitializationContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        eventStore.AppendCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Backfill should proceed when the store reports StreamNotFound")]
    public async Task ExecuteAsync_WhenReadFailsWithStreamNotFound_ShouldProceed()
    {
        // Arrange
        TodoReadModel row = await SeedRowAsync(version: 42);
        var eventStore = new Mock<IEventStore>();
        eventStore
            .Setup(store => store.ReadStreamAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<StoredEvent>>.Fail(
                EventSourcingOutcome.Failure.StreamNotFound("missing")));
        eventStore
            .Setup(store => store.AppendAsync(
                It.IsAny<string>(),
                0,
                It.IsAny<IReadOnlyCollection<IDomainEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<long>.Success(1));

        var snapshots = new RecordingSnapshotStore();
        var checkpoints = new RecordingCheckpointStore();
        TodoBackfillSeeder seeder = CreateSeeder(eventStore: eventStore.Object, snapshots: snapshots, checkpoints: checkpoints);

        // Act
        Result result = await seeder.ExecuteAsync(CreateInitializationContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        eventStore.Verify(
            store => store.AppendAsync(
                It.IsAny<string>(),
                0,
                It.IsAny<IReadOnlyCollection<IDomainEvent>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        (await ReadVersionAsync(row.Id)).Should().Be(0);
    }

    [Fact(DisplayName = "Backfill should return a non-StreamNotFound read failure")]
    public async Task ExecuteAsync_WhenReadFails_ShouldReturnTheFailure()
    {
        // Arrange
        TodoReadModel row = await SeedRowAsync(version: 42);
        var eventStore = new Mock<IEventStore>();
        eventStore
            .Setup(store => store.ReadStreamAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<StoredEvent>>.Fail(
                EventSourcingOutcome.Failure.ReadFailed("todo", "unavailable")));

        TodoBackfillSeeder seeder = CreateSeeder(eventStore: eventStore.Object);

        // Act
        Result result = await seeder.ExecuteAsync(CreateInitializationContext());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(EventSourcingOutcome.Codes.ReadFailed);
        eventStore.Verify(
            store => store.AppendAsync(
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<IReadOnlyCollection<IDomainEvent>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        (await ReadVersionAsync(row.Id)).Should().Be(42);
    }

    [Fact(DisplayName = "Backfill should propagate append, snapshot, and checkpoint failures")]
    public async Task ExecuteAsync_WhenAStoreFails_ShouldReturnTheFailure()
    {
        // Arrange: each failure needs its own row and store so a prior append cannot skip it.
        await SeedRowAsync();
        var appendStore = new InMemoryEventStore(new FakeClock(_now))
        {
            AppendFailure = EventSourcingOutcome.Failure.AppendFailed("todo", "boom"),
        };

        await SeedRowAsync();
        var snapshotStore = new InMemoryEventStore(new FakeClock(_now));
        var failingSnapshots = new RecordingSnapshotStore
        {
            Failure = EventSourcingOutcome.Failure.SnapshotWriteFailed("todo", "boom"),
        };

        await SeedRowAsync();
        var checkpointStore = new InMemoryEventStore(new FakeClock(_now));
        var failingCheckpoints = new RecordingCheckpointStore
        {
            Failure = EventSourcingOutcome.Failure.ProjectionCheckpointFailed("TodoBackfill", "boom"),
        };

        // Act
        Result append = await CreateSeeder(eventStore: appendStore)
            .ExecuteAsync(CreateInitializationContext());
        Result snapshot = await CreateSeeder(
            eventStore: snapshotStore, snapshots: failingSnapshots)
            .ExecuteAsync(CreateInitializationContext());
        Result checkpoint = await CreateSeeder(
            eventStore: checkpointStore, checkpoints: failingCheckpoints)
            .ExecuteAsync(CreateInitializationContext());

        // Assert
        append.IsFailure.Should().BeTrue();
        append.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(EventSourcingOutcome.Codes.AppendFailed);
        snapshot.IsFailure.Should().BeTrue();
        snapshot.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(EventSourcingOutcome.Codes.SnapshotWriteFailed);
        checkpoint.IsFailure.Should().BeTrue();
        checkpoint.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(EventSourcingOutcome.Codes.ProjectionCheckpointFailed);
    }

    [Fact(DisplayName = "Backfill should forward the cancellation token to every store")]
    public async Task ExecuteAsync_ShouldForwardTheCancellationToken()
    {
        // Arrange
        await SeedRowAsync();
        using var cancellation = new CancellationTokenSource();
        var eventStore = new Mock<IEventStore>();
        eventStore
            .Setup(store => store.ReadStreamAsync(It.IsAny<string>(), 1, cancellation.Token))
            .ReturnsAsync(Result<IReadOnlyList<StoredEvent>>.Success([]));
        eventStore
            .Setup(store => store.AppendAsync(
                It.IsAny<string>(),
                0,
                It.IsAny<IReadOnlyCollection<IDomainEvent>>(),
                cancellation.Token))
            .ReturnsAsync(Result<long>.Success(1));

        var snapshots = new Mock<ISnapshotStore>();
        snapshots
            .Setup(store => store.SaveAsync(It.IsAny<Snapshot>(), cancellation.Token))
            .ReturnsAsync(Result.Ok());
        var checkpoints = new Mock<IProjectionCheckpointStore>();
        checkpoints
            .Setup(store => store.SavePositionAsync(
                It.IsAny<string>(), It.IsAny<long>(), cancellation.Token))
            .ReturnsAsync(Result.Ok());

        TodoBackfillSeeder seeder = CreateSeeder(
            eventStore: eventStore.Object,
            snapshots: snapshots.Object,
            checkpoints: checkpoints.Object);

        // Act
        Result result = await seeder.ExecuteAsync(
            CreateInitializationContext(), cancellation.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        eventStore.Verify(
            store => store.ReadStreamAsync(It.IsAny<string>(), 1, cancellation.Token),
            Times.Once);
        eventStore.Verify(
            store => store.AppendAsync(
                It.IsAny<string>(),
                0,
                It.IsAny<IReadOnlyCollection<IDomainEvent>>(),
                cancellation.Token),
            Times.Once);
        snapshots.Verify(
            store => store.SaveAsync(It.IsAny<Snapshot>(), cancellation.Token),
            Times.Once);
        checkpoints.Verify(
            store => store.SavePositionAsync(
                It.IsAny<string>(), It.IsAny<long>(), cancellation.Token),
            Times.Once);
    }

    [Fact(DisplayName = "Preview should report the disabled state")]
    public async Task PreviewAsync_WhenDisabled_ShouldReportDataCheck()
    {
        // Arrange
        await SeedRowAsync();
        TodoBackfillSeeder seeder = CreateSeeder(enabled: false);

        // Act
        Result<IReadOnlyList<InitializationOperation>> result =
            await seeder.PreviewAsync(CreateInitializationContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Type.Should().Be(InitializationOperationType.DataCheck);
    }

    [Fact(DisplayName = "Preview should list only rows without a stream")]
    public async Task PreviewAsync_WhenEnabled_ShouldListMissingStreams()
    {
        // Arrange
        TodoReadModel missing = await SeedRowAsync();
        TodoReadModel existing = await SeedRowAsync();
        var eventStore = new InMemoryEventStore();
        string existingStream = EventStreamName.For<Todo>(existing.Id);
        await eventStore.AppendAsync(
            existingStream,
            0,
            [new TodoCreated(existing.Id, existing.TenantId, existing.Title, null, existing.Priority, null, null, existing.CreatedAtUtc, existing.CreatedBy)]);

        var checkpoints = new RecordingCheckpointStore();
        checkpoints.Positions[$"TodoBackfill:{existing.Id}"] = 1;
        TodoBackfillSeeder seeder = CreateSeeder(eventStore: eventStore, checkpoints: checkpoints);

        // Act
        Result<IReadOnlyList<InitializationOperation>> result =
            await seeder.PreviewAsync(CreateInitializationContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Description.Should().Contain(EventStreamName.For<Todo>(missing.Id));
        result.Value.Single().Type.Should().Be(InitializationOperationType.DataChange);
    }

    [Fact(DisplayName = "Preview should report a data check when every stream exists")]
    public async Task PreviewAsync_WhenNothingToBackfill_ShouldReportDataCheck()
    {
        // Arrange
        TodoReadModel row = await SeedRowAsync();
        var eventStore = new InMemoryEventStore();
        string streamId = EventStreamName.For<Todo>(row.Id);
        await eventStore.AppendAsync(
            streamId,
            0,
            [new TodoCreated(row.Id, row.TenantId, row.Title, null, row.Priority, null, null, row.CreatedAtUtc, row.CreatedBy)]);

        var checkpoints = new RecordingCheckpointStore();
        checkpoints.Positions[$"TodoBackfill:{row.Id}"] = 1;
        TodoBackfillSeeder seeder = CreateSeeder(eventStore: eventStore, checkpoints: checkpoints);

        // Act
        Result<IReadOnlyList<InitializationOperation>> result =
            await seeder.PreviewAsync(CreateInitializationContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Type.Should().Be(InitializationOperationType.DataCheck);
    }

    [Fact(DisplayName = "Preview should report marker recovery when a stream has no marker")]
    public async Task PreviewAsync_WhenMarkerIsMissing_ShouldReportRecovery()
    {
        // Arrange
        TodoReadModel row = await SeedRowAsync();
        var eventStore = new InMemoryEventStore();
        string streamId = EventStreamName.For<Todo>(row.Id);
        await eventStore.AppendAsync(
            streamId,
            0,
            [new TodoCreated(row.Id, row.TenantId, row.Title, null, row.Priority, null, null, row.CreatedAtUtc, row.CreatedBy)]);

        TodoBackfillSeeder seeder = CreateSeeder(eventStore: eventStore);

        // Act
        Result<IReadOnlyList<InitializationOperation>> result =
            await seeder.PreviewAsync(CreateInitializationContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Description.Should().Contain(streamId)
            .And.Contain("marker");
        result.Value.Single().Type.Should().Be(InitializationOperationType.DataChange);
    }

    private TodoBackfillSeeder CreateSeeder(
        bool enabled = true,
        IEventStore? eventStore = null,
        ISnapshotStore? snapshots = null,
        IProjectionCheckpointStore? checkpoints = null,
        int batchSize = 500)
        => new(
            _context,
            eventStore ?? new InMemoryEventStore(),
            snapshots ?? new RecordingSnapshotStore(),
            checkpoints ?? new RecordingCheckpointStore(),
            Options.Create(new EventStoreSetting { BackfillTodos = enabled, BatchSize = batchSize }),
            new FakeClock(_now));

    private static IInitializationContext CreateInitializationContext()
    {
        var context = new Mock<IInitializationContext>();
        context.SetupGet(initialization => initialization.Logger).Returns(Mock.Of<ILogger>());
        return context.Object;
    }

    private async Task<TodoReadModel> SeedRowAsync(
        Action<TodoReadModel>? configure = null,
        long version = 7)
    {
        var row = new TodoReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Title = "Legacy todo",
            Status = TodoStatus.Pending,
            Priority = TodoPriority.Medium,
            CreatedAtUtc = _now,
            CreatedBy = "creator",
            Version = version,
        };

        configure?.Invoke(row);

        _context.Set<TodoReadModel>().Add(row);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return row;
    }

    private async Task<long> ReadVersionAsync(Guid id)
    {
        _context.ChangeTracker.Clear();
        return await _context.Set<TodoReadModel>()
            .AsNoTracking()
            .Where(todo => todo.Id == id)
            .Select(todo => todo.Version)
            .SingleAsync();
    }

    /// <summary>
    /// Minimal SQLite context for the todos read model. SQLite cannot order
    /// <see cref="DateTimeOffset"/> columns directly, so the created timestamp is stored
    /// round-trip formatted and ordered as a string.
    /// </summary>
    private sealed class BackfillTestContext(DbContextOptions<BackfillTestContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TodoReadModelConfiguration());
            modelBuilder.Entity<TodoReadModel>()
                .Property(todo => todo.CreatedAtUtc)
                .HasConversion(
                    value => value.ToString("O", CultureInfo.InvariantCulture),
                    value => DateTimeOffset.Parse(
                        value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
            modelBuilder.Ignore<TodoList>();
            modelBuilder.Ignore<TodoCategory>();
        }
    }

    /// <summary>Clock fixed to the test timestamp.</summary>
    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>In-memory event store that enforces the expected stream version.</summary>
    private sealed class InMemoryEventStore(TimeProvider? clock = null) : IEventStore
    {
        private readonly Dictionary<string, List<IDomainEvent>> _streams = [];
        private readonly TimeProvider _clock = clock ?? TimeProvider.System;

        public int AppendCalls { get; private set; }

        public List<long> ExpectedVersions { get; } = [];

        public Error? AppendFailure { get; set; }

        public Func<Task>? BeforeAppend { get; set; }

        public async Task<Result<long>> AppendAsync(
            string streamId,
            long expectedVersion,
            IReadOnlyCollection<IDomainEvent> events,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            AppendCalls++;
            ExpectedVersions.Add(expectedVersion);

            if (BeforeAppend is not null)
                await BeforeAppend();

            if (AppendFailure is not null)
                return Result<long>.Fail(AppendFailure);

            if (!_streams.TryGetValue(streamId, out List<IDomainEvent>? stream))
            {
                stream = [];
                _streams[streamId] = stream;
            }

            if (stream.Count != expectedVersion)
                return Result<long>.Fail(
                    EventSourcingOutcome.Failure.AppendConflict(streamId, expectedVersion));

            stream.AddRange(events);
            return Result<long>.Success(stream.Count);
        }

        public async Task<Result<IReadOnlyList<StoredEvent>>> ReadStreamAsync(
            string streamId,
            long fromVersion,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (!_streams.TryGetValue(streamId, out List<IDomainEvent>? stream))
                return Result<IReadOnlyList<StoredEvent>>.Success([]);

            List<StoredEvent> stored = [];
            for (int index = 0; index < stream.Count; index++)
            {
                IDomainEvent domainEvent = stream[index];
                stored.Add(new StoredEvent(
                    streamId,
                    index + 1,
                    domainEvent.GetEventId() ?? Guid.NewGuid(),
                    domainEvent.EventName,
                    domainEvent.EventVersion,
                    "{}",
                    new DefaultMetadata(),
                    _clock.GetUtcNow(),
                    index + 1));
            }

            return Result<IReadOnlyList<StoredEvent>>.Success(stored);
        }

        public Task<Result<IReadOnlyList<StoredEvent>>> ReadAllAsync(
            long fromPosition,
            int batchSize,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result<IReadOnlyList<StoredEvent>>.Success([]));

        public string SingleStreamId() => _streams.Keys.Should().ContainSingle().Subject;

        public List<IDomainEvent> Events(string streamId)
            => _streams.TryGetValue(streamId, out List<IDomainEvent>? stream) ? stream : [];
    }

    /// <summary>Snapshot store that records saved snapshots.</summary>
    private sealed class RecordingSnapshotStore : ISnapshotStore
    {
        public List<Snapshot> Saved { get; } = [];

        public Error? Failure { get; set; }

        public Task<Result<Maybe<Snapshot>>> LoadAsync(
            string streamId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result<Maybe<Snapshot>>.Success(Maybe<Snapshot>.Empty()));

        public Task<Result> SaveAsync(
            Snapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            if (Failure is not null)
                return Task.FromResult(Result.Fail(Failure));

            Saved.Add(snapshot);
            return Task.FromResult(Result.Ok());
        }
    }

    /// <summary>Checkpoint store that records positions.</summary>
    private sealed class RecordingCheckpointStore : IProjectionCheckpointStore
    {
        public Dictionary<string, long> Positions { get; } = [];

        public int SaveCalls { get; private set; }

        public Error? Failure { get; set; }

        public Task<Result<long>> GetPositionAsync(
            string projection,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result<long>.Success(
                Positions.TryGetValue(projection, out long position) ? position : 0));

        public Task<Result> SavePositionAsync(
            string projection,
            long position,
            CancellationToken cancellationToken = default)
        {
            if (Failure is not null)
                return Task.FromResult(Result.Fail(Failure));

            SaveCalls++;
            Positions[projection] = position;
            return Task.FromResult(Result.Ok());
        }
    }
}

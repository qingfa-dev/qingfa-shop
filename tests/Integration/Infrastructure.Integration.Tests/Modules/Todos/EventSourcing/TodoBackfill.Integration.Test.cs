using System.Data;

using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Messaging.Serialization;
using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Persistence.UnitOfWorks;
using Application.Modules.Todos.Items.Models;

using Core.BuildingBlocks.Domain.Events;
using Core.BuildingBlocks.Functional.Maybes;
using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Todos.Items;

using Infrastructure.Integration.Tests.BuildingBlocks.Messaging;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Initialization;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Projections;
using Infrastructure.Modules.Todos.Persistence.EventSourcing.Options;
using Infrastructure.Modules.Todos.Persistence.Projections;
using Infrastructure.Modules.Todos.Persistence.Schemas.Seeders;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integration.Tests.Modules.Todos.EventSourcing;

/// <summary>PostgreSQL coverage for the legacy todo backfill.</summary>
[Collection("messaging")]
public sealed class TodoBackfillIntegrationTest(MessagingFixture fixture) : IAsyncLifetime
{
    private readonly DateTimeOffset _now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await fixture.CleanAsync();
        fixture.CreateScope();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await fixture.CleanAsync();
    }

    [Fact(DisplayName = "Backfill should reset the legacy version and replay into the read model")]
    public async Task Backfill_ShouldResetVersionThenProjectTheSeededRow()
    {
        // Arrange: a legacy row with an aggregate version that is not a stream version.
        ApplicationDbContext db = fixture.DbContext;
        IServiceProvider scoped = fixture.CurrentScope!.ServiceProvider;
        IEventStore eventStore = scoped.GetRequiredService<IEventStore>();
        ISnapshotStore snapshots = scoped.GetRequiredService<ISnapshotStore>();
        IProjectionCheckpointStore checkpoints = scoped.GetRequiredService<IProjectionCheckpointStore>();
        IDomainEventSerializer serializer = fixture.DomainSerializer;

        TodoReadModel row = CreateLegacyRow();
        row.CompletedAtUtc.Should().NotBe(row.ModifiedAtUtc);
        db.Set<TodoReadModel>().Add(row);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        TodoBackfillSeeder seeder = CreateSeeder(db, eventStore, snapshots, checkpoints);
        IInitializationContext initialization = CreateInitializationContext(db, fixture.UnitOfWork);

        // Act
        Result first = await seeder.ExecuteAsync(initialization);

        // Assert: the reset happens before the projection resumes, and the stream is complete.
        first.IsSuccess.Should().BeTrue();
        (await ReadVersionAsync(db, row.Id)).Should().Be(0);

        string streamId = EventStreamName.For<Todo>(row.Id);
        Result<IReadOnlyList<StoredEvent>> stream = await eventStore.ReadStreamAsync(streamId, 1);
        stream.IsSuccess.Should().BeTrue();
        stream.Value.Should().HaveCount(7);

        Result<Maybe<Snapshot>> snapshot = await snapshots.LoadAsync(streamId);
        snapshot.IsSuccess.Should().BeTrue();
        snapshot.Value.HasValue.Should().BeTrue();
        snapshot.Value.Value.Version.Should().Be(7);

        Result<long> marker = await checkpoints.GetPositionAsync($"TodoBackfill:{row.Id}");
        marker.IsSuccess.Should().BeTrue();
        marker.Value.Should().Be(7);

        // Act: replay the stream through the real PostgreSQL projection.
        var runner = new ProjectionRunner(
            eventStore,
            [new TodoProjection(db, serializer)],
            checkpoints,
            new EventStoreSetting { BatchSize = 500 },
            NullLogger<ProjectionRunner>.Instance);
        Result projected = await runner.RunOnceAsync();

        // Assert: every field converges to the legacy row.
        projected.IsSuccess.Should().BeTrue();
        db.ChangeTracker.Clear();
        TodoReadModel replayed = await db.Set<TodoReadModel>().SingleAsync(todo => todo.Id == row.Id);
        replayed.Version.Should().Be(7);
        replayed.TenantId.Should().Be(row.TenantId);
        replayed.Title.Should().Be(row.Title);
        replayed.Description.Should().Be(row.Description);
        replayed.Status.Should().Be(TodoStatus.Completed);
        replayed.Priority.Should().Be(TodoPriority.High);
        replayed.DueAtUtc.Should().Be(row.DueAtUtc);
        replayed.CompletedAtUtc.Should().Be(_now.AddMinutes(45));
        replayed.CompletedAtUtc.Should().Be(row.CompletedAtUtc);
        replayed.ListId.Should().Be(row.ListId);
        replayed.CategoryIds.Should().Equal(row.CategoryIds);
        replayed.CreatedAtUtc.Should().Be(row.CreatedAtUtc);
        replayed.CreatedBy.Should().Be(row.CreatedBy);
        replayed.ModifiedAtUtc.Should().Be(_now.AddMinutes(20));
        replayed.ModifiedAtUtc.Should().Be(row.ModifiedAtUtc);
        replayed.ModifiedBy.Should().Be(row.ModifiedBy);
        replayed.IsDeleted.Should().BeTrue();
        replayed.DeletedAtUtc.Should().Be(row.DeletedAtUtc);
        replayed.DeletedBy.Should().Be(row.DeletedBy);

        // Act: run the seeder again after the projection.
        Result second = await seeder.ExecuteAsync(initialization);

        // Assert: the existing stream is skipped, so no duplicate events appear.
        second.IsSuccess.Should().BeTrue();
        Result<IReadOnlyList<StoredEvent>> unchanged = await eventStore.ReadStreamAsync(streamId, 1);
        unchanged.IsSuccess.Should().BeTrue();
        unchanged.Value.Should().HaveCount(7);
        (await ReadVersionAsync(db, row.Id)).Should().Be(7);
    }

    [Fact(DisplayName = "Backfill should recover only the marker after a partial run")]
    public async Task Backfill_WhenMarkerIsMissing_ShouldRecoverOnlyTheMarker()
    {
        // Arrange
        ApplicationDbContext db = fixture.DbContext;
        IServiceProvider scoped = fixture.CurrentScope!.ServiceProvider;
        IEventStore eventStore = scoped.GetRequiredService<IEventStore>();
        ISnapshotStore snapshots = scoped.GetRequiredService<ISnapshotStore>();
        IProjectionCheckpointStore checkpoints = scoped.GetRequiredService<IProjectionCheckpointStore>();

        TodoReadModel row = CreateLegacyRow();
        row.CompletedAtUtc.Should().NotBe(row.ModifiedAtUtc);
        db.Set<TodoReadModel>().Add(row);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        TodoBackfillSeeder seeder = CreateSeeder(db, eventStore, snapshots, checkpoints);
        IInitializationContext initialization = CreateInitializationContext(db, fixture.UnitOfWork);
        string streamId = EventStreamName.For<Todo>(row.Id);
        string marker = $"TodoBackfill:{row.Id}";

        (await seeder.ExecuteAsync(initialization)).IsSuccess.Should().BeTrue();

        // Arrange: simulate a crash after the append but before the marker was durable.
        // The snapshot is removed as well to prove recovery must not rebuild it from the
        // read model, which can lag the stream.
        await db.Set<Snapshot>()
            .Where(snapshot => snapshot.StreamId == streamId)
            .ExecuteDeleteAsync();
        (await checkpoints.SavePositionAsync(marker, 0)).IsSuccess.Should().BeTrue();
        db.ChangeTracker.Clear();

        // Act
        Result recovered = await seeder.ExecuteAsync(initialization);

        // Assert: the stream is untouched, the marker is restored, and no snapshot is written.
        recovered.IsSuccess.Should().BeTrue();
        Result<IReadOnlyList<StoredEvent>> stream = await eventStore.ReadStreamAsync(streamId, 1);
        stream.IsSuccess.Should().BeTrue();
        stream.Value.Should().HaveCount(7);

        Result<Maybe<Snapshot>> snapshot = await snapshots.LoadAsync(streamId);
        snapshot.IsSuccess.Should().BeTrue();
        snapshot.Value.HasValue.Should().BeFalse();

        Result<long> restoredMarker = await checkpoints.GetPositionAsync(marker);
        restoredMarker.IsSuccess.Should().BeTrue();
        restoredMarker.Value.Should().Be(7);
        (await ReadVersionAsync(db, row.Id)).Should().Be(0);
    }

    private TodoReadModel CreateLegacyRow()
    {
        Guid listId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid secondCategoryId = Guid.NewGuid();

        return new TodoReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Title = "Legacy todo",
            Description = "Backfilled from the pre-cutover table",
            Status = TodoStatus.Completed,
            Priority = TodoPriority.High,
            DueAtUtc = _now.AddDays(1),
            // Distinct completion and modification stamps prove the trailing update repairs
            // the audit values that TodoStatusChanged would otherwise overwrite.
            CompletedAtUtc = _now.AddMinutes(45),
            ListId = listId,
            CategoryIds = [categoryId, secondCategoryId],
            CreatedAtUtc = _now,
            CreatedBy = "creator",
            ModifiedAtUtc = _now.AddMinutes(20),
            ModifiedBy = "editor",
            IsDeleted = true,
            DeletedAtUtc = _now.AddHours(1),
            DeletedBy = "deleter",
            Version = 999,
        };
    }

    private static TodoBackfillSeeder CreateSeeder(
        ApplicationDbContext db,
        IEventStore eventStore,
        ISnapshotStore snapshots,
        IProjectionCheckpointStore checkpoints)
        => new(
            db,
            eventStore,
            snapshots,
            checkpoints,
            Options.Create(new EventStoreSetting { BatchSize = 500 }),
            Options.Create(new TodoEventStoreSetting { BackfillTodos = true }),
            TimeProvider.System);

    private static IInitializationContext CreateInitializationContext(
        ApplicationDbContext db,
        IUnitOfWork unitOfWork)
    {
        IDbConnection connection = db.Database.GetDbConnection();
        return new EfCoreInitializationContext(connection, unitOfWork, NullLogger.Instance);
    }

    private static async Task<long> ReadVersionAsync(ApplicationDbContext db, Guid id)
    {
        db.ChangeTracker.Clear();
        return await db.Set<TodoReadModel>()
            .AsNoTracking()
            .Where(todo => todo.Id == id)
            .Select(todo => todo.Version)
            .SingleAsync();
    }
}

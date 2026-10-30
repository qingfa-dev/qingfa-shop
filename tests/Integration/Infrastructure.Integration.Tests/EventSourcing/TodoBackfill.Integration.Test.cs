using System.Data;

using Application.Common.EventSourcing;
using Application.Common.Messaging.Serialization;
using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Items.Models;

using Core.Common.Domain.Events;
using Core.Common.Monad.Maybes;
using Core.Common.Monad.Results;
using Core.Todos.Items;

using Infrastructure.Integration.Tests.Messaging;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.Persistence.EntityFramework.Initialization;
using Infrastructure.Persistence.EntityFramework.Projections;
using Infrastructure.Persistence.Schemas.Todos.Seeders;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integration.Tests.EventSourcing;

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
        stream.Value.Should().HaveCount(6);

        Result<Maybe<Snapshot>> snapshot = await snapshots.LoadAsync(streamId);
        snapshot.IsSuccess.Should().BeTrue();
        snapshot.Value.HasValue.Should().BeTrue();
        snapshot.Value.Value.Version.Should().Be(6);

        Result<long> marker = await checkpoints.GetPositionAsync($"TodoBackfill:{row.Id}");
        marker.IsSuccess.Should().BeTrue();
        marker.Value.Should().Be(6);

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
        replayed.Version.Should().Be(6);
        replayed.TenantId.Should().Be(row.TenantId);
        replayed.Title.Should().Be(row.Title);
        replayed.Description.Should().Be(row.Description);
        replayed.Status.Should().Be(TodoStatus.Completed);
        replayed.Priority.Should().Be(TodoPriority.High);
        replayed.DueAtUtc.Should().Be(row.DueAtUtc);
        replayed.CompletedAtUtc.Should().Be(row.CompletedAtUtc);
        replayed.ListId.Should().Be(row.ListId);
        replayed.CategoryIds.Should().Equal(row.CategoryIds);
        replayed.CreatedAtUtc.Should().Be(row.CreatedAtUtc);
        replayed.CreatedBy.Should().Be(row.CreatedBy);
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
        unchanged.Value.Should().HaveCount(6);
        (await ReadVersionAsync(db, row.Id)).Should().Be(6);
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
            CompletedAtUtc = _now.AddMinutes(30),
            ListId = listId,
            CategoryIds = [categoryId, secondCategoryId],
            CreatedAtUtc = _now,
            CreatedBy = "creator",
            ModifiedAtUtc = _now.AddMinutes(30),
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
            Options.Create(new EventStoreSetting { BackfillTodos = true, BatchSize = 500 }),
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

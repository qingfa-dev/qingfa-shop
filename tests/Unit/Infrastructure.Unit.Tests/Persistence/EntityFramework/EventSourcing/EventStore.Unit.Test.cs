using Application.Common.EventSourcing;
using Application.Common.Messaging.Serialization;

using Core.Common.Domain.Events;

using Infrastructure.Integrations.Messaging.Serialization;
using Infrastructure.Persistence.EntityFramework.EventSourcing;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.EventSourcing;

/// <summary>SQLite-backed unit tests for event-store persistence logic.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "EventSourcing")]
public sealed class EventStoreUnitTest : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private EventStoreTestContext _context = null!;
    private PostgresEventStore _store = null!;

    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<EventStoreTestContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new EventStoreTestContext(options);
        await _context.Database.EnsureCreatedAsync();
        _store = new PostgresEventStore(
            _context,
            new JsonMessageSerializer(new EventTypeRegistry(typeof(DomainEvent).Assembly)),
            TimeProvider.System);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task SnapshotsAndCheckpoints_ShouldRoundTrip()
    {
        var snapshots = new EfCoreSnapshotStore(_context);
        var checkpoints = new EfCoreProjectionCheckpointStore(_context);
        string stream = $"test-{Guid.NewGuid():N}";
        string projection = $"projection-{Guid.NewGuid():N}";

        Result savedSnapshot = await snapshots.SaveAsync(
            new Snapshot(stream, 5, "{}", DateTimeOffset.UtcNow));
        Result<Core.Common.Monad.Maybes.Maybe<Snapshot>> loaded = await snapshots.LoadAsync(stream);
        Result savedCheckpoint = await checkpoints.SavePositionAsync(projection, 9);
        Result<long> loadedCheckpoint = await checkpoints.GetPositionAsync(projection);

        savedSnapshot.IsSuccess.Should().BeTrue();
        loaded.Value.Value.Version.Should().Be(5);
        savedCheckpoint.IsSuccess.Should().BeTrue();
        loadedCheckpoint.Value.Should().Be(9);
    }

    private sealed record StoreEvent(string Value) : DomainEvent;

    private sealed class EventStoreTestContext(DbContextOptions<EventStoreTestContext> options)
        : Microsoft.EntityFrameworkCore.DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new EventStoreEventConfiguration());
            modelBuilder.ApplyConfiguration(new EventStoreSnapshotConfiguration());
            modelBuilder.ApplyConfiguration(new ProjectionCheckpointConfiguration());
        }
    }
}

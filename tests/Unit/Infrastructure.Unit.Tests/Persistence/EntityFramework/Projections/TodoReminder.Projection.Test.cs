using Application.Common.EventSourcing;
using Application.Common.Messaging.Serialization;
using Application.Features.Todos.Reminders.Models;

using Core.Common.Domain.Events;
using Core.Todos.Items;
using Core.Todos.Reminders;

using Infrastructure.Integrations.Messaging.Serialization;
using Infrastructure.Persistence.EntityFramework.Projections;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Projections;

/// <summary>Tests for the reminder read-model projection.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "EventSourcing")]
public sealed class TodoReminderProjectionTest : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ProjectionTestContext _context = null!;
    private JsonMessageSerializer _serializer = null!;
    private TodoReminderProjection _projection = null!;
    private readonly DateTimeOffset _now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ProjectionTestContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ProjectionTestContext(options);
        await _context.Database.EnsureCreatedAsync();
        _serializer = new JsonMessageSerializer(new EventTypeRegistry(typeof(DomainEvent).Assembly));
        _projection = new TodoReminderProjection(_context, _serializer);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Apply_ShouldProjectScheduleSnoozeAndDismiss()
    {
        Guid id = Guid.NewGuid();
        Guid todoId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();

        await _projection.ApplyAsync(Stored(new ReminderScheduled(id, todoId, tenantId, _now.AddHours(1)), 1));
        TodoReminderModel model = await _context.Reminders.SingleAsync();
        model.Status.Should().Be(ReminderStatus.Scheduled);

        await _projection.ApplyAsync(Stored(new ReminderSnoozed(id, todoId, tenantId, _now.AddHours(2), 1), 2));
        await _context.Entry(model).ReloadAsync();
        model.Status.Should().Be(ReminderStatus.Snoozed);
        model.SnoozeCount.Should().Be(1);

        await _projection.ApplyAsync(Stored(new ReminderDismissed(id, todoId, tenantId, _now.AddHours(1), _now.AddHours(3)), 3));
        await _context.Entry(model).ReloadAsync();
        model.Status.Should().Be(ReminderStatus.Dismissed);
        model.Version.Should().Be(3);
    }

    [Fact]
    public async Task Apply_ShouldIgnoreEventsOlderThanTheProjectedVersion()
    {
        Guid id = Guid.NewGuid();
        Guid todoId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();

        await _projection.ApplyAsync(Stored(new ReminderScheduled(id, todoId, tenantId, _now.AddHours(1)), 1));
        await _projection.ApplyAsync(Stored(new ReminderSnoozed(id, todoId, tenantId, _now.AddHours(2), 1), 2));
        await _projection.ApplyAsync(
            Stored(new ReminderDismissed(id, todoId, tenantId, _now.AddHours(2), _now.AddHours(3)), 3));

        // Replay an older delivery and the creation event; neither may regress the row.
        await _projection.ApplyAsync(Stored(new ReminderSnoozed(id, todoId, tenantId, _now.AddHours(4), 2), 2));
        await _projection.ApplyAsync(Stored(new ReminderScheduled(id, todoId, tenantId, _now.AddHours(5)), 1));

        TodoReminderModel model = await _context.Reminders.SingleAsync();
        model.Status.Should().Be(ReminderStatus.Dismissed);
        model.Version.Should().Be(3);
        model.RemindAtUtc.Should().Be(_now.AddHours(2));
    }

    [Fact]
    public async Task Apply_WithAnUnrelatedEvent_ShouldBeANoOp()
    {
        string name = _projection.Name;

        Result result = await _projection.ApplyAsync(
            Stored(
                new TodoStatusChanged(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    TodoStatus.Pending,
                    TodoStatus.Completed,
                    _now,
                    "test"),
                1));

        name.Should().Be("todo-reminders");
        result.IsSuccess.Should().BeTrue();
        (await _context.Reminders.CountAsync()).Should().Be(0);
    }

    private StoredEvent Stored(IDomainEvent domainEvent, long version)
        => new(
            $"todo-reminder:{domainEvent.GetType().Name}",
            version,
            domainEvent.GetEventId() ?? Guid.NewGuid(),
            domainEvent.EventName,
            domainEvent.EventVersion,
            _serializer.Serialize(domainEvent),
            domainEvent.Metadata,
            _now,
            version);

    private sealed class ProjectionTestContext(DbContextOptions<ProjectionTestContext> options)
        : Microsoft.EntityFrameworkCore.DbContext(options)
    {
        public DbSet<TodoReminderModel> Reminders => Set<TodoReminderModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.ApplyConfiguration(new TodoReminderReadModelConfiguration());
    }
}

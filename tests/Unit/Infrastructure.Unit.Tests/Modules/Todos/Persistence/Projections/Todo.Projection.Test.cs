using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Messaging.Serialization;
using Application.Modules.Todos.Items.Models;

using Core.BuildingBlocks.Domain.Events;
using Core.Modules.Todos.Categories;
using Core.Modules.Todos.Items;
using Core.Modules.Todos.Lists;
using Core.Modules.Todos.Reminders;

using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Interceptors;
using Infrastructure.Modules.Todos.Persistence.Projections;
using Infrastructure.Modules.Todos.Persistence.Schemas.EntityConfiguration;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Modules.Todos.Persistence.Projections;

/// <summary>Tests for the todo read-model projection.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "EventSourcing")]
public sealed class TodoProjectionTest : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ProjectionTestContext _context = null!;
    private JsonMessageSerializer _serializer = null!;
    private TodoProjection _projection = null!;
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
        _projection = new TodoProjection(_context, _serializer);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Apply_WithTodoCreated_ShouldInsertAPendingRow()
    {
        Guid id = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid listId = Guid.NewGuid();

        Result result = await _projection.ApplyAsync(
            Stored(
                new TodoCreated(
                    id,
                    tenantId,
                    "Write report",
                    "Draft the quarterly report",
                    TodoPriority.High,
                    _now.AddDays(1),
                    listId,
                    _now,
                    "creator"),
                1));

        result.IsSuccess.Should().BeTrue();
        TodoReadModel model = await ReadPersistedAsync();
        model.Id.Should().Be(id);
        model.TenantId.Should().Be(tenantId);
        model.Title.Should().Be("Write report");
        model.Description.Should().Be("Draft the quarterly report");
        model.Status.Should().Be(TodoStatus.Pending);
        model.Priority.Should().Be(TodoPriority.High);
        model.DueAtUtc.Should().Be(_now.AddDays(1));
        model.CompletedAtUtc.Should().BeNull();
        model.ListId.Should().Be(listId);
        model.CategoryIds.Should().BeEmpty();
        model.IsCompleted.Should().BeFalse();
        model.CreatedAtUtc.Should().Be(_now);
        model.CreatedBy.Should().Be("creator");
        model.IsDeleted.Should().BeFalse();
        model.Version.Should().Be(1);
    }

    [Fact]
    public async Task Apply_ShouldMutateTheRowForEveryTodoEvent()
    {
        Guid id = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid listId = Guid.NewGuid();
        await CreateAsync(id, tenantId);

        await _projection.ApplyAsync(
            Stored(
                new TodoUpdated(
                    id,
                    "Write final report",
                    "Final draft",
                    TodoPriority.Low,
                    _now.AddDays(2),
                    listId,
                    _now.AddMinutes(1),
                    "editor"),
                2));

        await _projection.ApplyAsync(
            Stored(
                new TodoStatusChanged(
                    id,
                    tenantId,
                    TodoStatus.Pending,
                    TodoStatus.Completed,
                    _now.AddMinutes(2),
                    "editor"),
                3));

        await _projection.ApplyAsync(
            Stored(new TodoCategoryAdded(id, categoryId, _now.AddMinutes(3), "editor"), 4));

        await _projection.ApplyAsync(
            Stored(new TodoCategoryRemoved(id, categoryId, _now.AddMinutes(4), "editor"), 5));

        Guid nextCategoryId = Guid.NewGuid();
        await _projection.ApplyAsync(
            Stored(new TodoCategoryAdded(id, nextCategoryId, _now.AddMinutes(5), "editor"), 6));

        await _projection.ApplyAsync(
            Stored(new TodoListChanged(id, null, _now.AddMinutes(6), "editor"), 7));

        TodoReadModel model = await ReadPersistedAsync();
        model.Title.Should().Be("Write final report");
        model.Description.Should().Be("Final draft");
        model.Priority.Should().Be(TodoPriority.Low);
        model.DueAtUtc.Should().Be(_now.AddDays(2));
        model.Status.Should().Be(TodoStatus.Completed);
        model.CompletedAtUtc.Should().Be(_now.AddMinutes(2));
        model.IsCompleted.Should().BeTrue();
        model.CategoryIds.Should().Equal(nextCategoryId);
        model.ListId.Should().BeNull();
        model.ModifiedAtUtc.Should().Be(_now.AddMinutes(6));
        model.ModifiedBy.Should().Be("editor");
        model.Version.Should().Be(7);
    }

    [Fact]
    public async Task Apply_WithSoftDeleteAndRestore_ShouldToggleTheLifecycleFlags()
    {
        Guid id = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        await CreateAsync(id, tenantId);

        await _projection.ApplyAsync(
            Stored(new TodoDeleted(id, false, _now.AddMinutes(1), "deleter"), 2));

        TodoReadModel model = await ReadPersistedAsync();
        model.IsDeleted.Should().BeTrue();
        model.DeletedAtUtc.Should().Be(_now.AddMinutes(1));
        model.DeletedBy.Should().Be("deleter");
        model.Version.Should().Be(2);

        await _projection.ApplyAsync(
            Stored(new TodoRestored(id, _now.AddMinutes(2), "restorer"), 3));

        model = await ReadPersistedAsync();
        model.IsDeleted.Should().BeFalse();
        model.DeletedAtUtc.Should().BeNull();
        model.DeletedBy.Should().BeNull();
        model.ModifiedAtUtc.Should().Be(_now.AddMinutes(2));
        model.ModifiedBy.Should().Be("restorer");
        model.Version.Should().Be(3);
    }

    [Fact]
    public async Task Apply_WithHardDelete_ShouldRemoveTheRow()
    {
        Guid id = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        await CreateAsync(id, tenantId);

        await _projection.ApplyAsync(
            Stored(new TodoDeleted(id, true, _now.AddMinutes(1), "deleter"), 2));

        _context.ChangeTracker.Clear();
        (await _context.Todos.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Apply_WithHardDelete_ShouldBypassTheSoftDeleteInterceptor()
    {
        // Arrange: the lifecycle interceptor rewrites tracked deletes into soft deletes.
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ProjectionTestContext>()
            .UseSqlite(connection)
            .AddInterceptors(new LifecycleInterceptor())
            .Options;
        await using var context = new ProjectionTestContext(options);
        await context.Database.EnsureCreatedAsync();

        Guid id = Guid.NewGuid();
        context.Todos.Add(new TodoReadModel
        {
            Id = id,
            TenantId = Guid.NewGuid(),
            Title = "Write report",
            Status = TodoStatus.Pending,
            Priority = TodoPriority.High,
            CreatedAtUtc = _now,
        });
        await context.SaveChangesAsync();

        var projection = new TodoProjection(context, _serializer);

        // Act
        Result result = await projection.ApplyAsync(
            Stored(new TodoDeleted(id, true, _now.AddMinutes(1), "deleter"), 2));

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.ChangeTracker.Clear();
        (await context.Todos.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Apply_ShouldIgnoreEventsOlderThanTheProjectedVersion()
    {
        Guid id = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        await CreateAsync(id, tenantId);

        await _projection.ApplyAsync(
            Stored(
                new TodoUpdated(
                    id,
                    "Write final report",
                    "Final draft",
                    TodoPriority.Low,
                    null,
                    null,
                    _now.AddMinutes(1),
                    "editor"),
                2));

        // Replay: older and equal deliveries must not regress the row.
        await _projection.ApplyAsync(Stored(new TodoCreated(id, tenantId, "Original", null, TodoPriority.High, null, null, _now, "creator"), 1));
        await _projection.ApplyAsync(
            Stored(
                new TodoUpdated(
                    id,
                    "Stale title",
                    null,
                    TodoPriority.High,
                    null,
                    null,
                    _now.AddMinutes(5),
                    "editor"),
                2));

        TodoReadModel model = await ReadPersistedAsync();
        model.Title.Should().Be("Write final report");
        model.Priority.Should().Be(TodoPriority.Low);
        model.Version.Should().Be(2);
    }

    [Fact]
    public async Task Apply_WithNewerTodoCreated_ShouldResetStaleAuditAndState()
    {
        Guid id = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid originalListId = Guid.NewGuid();
        Guid newListId = Guid.NewGuid();
        await CreateAsync(id, tenantId);

        await _projection.ApplyAsync(
            Stored(new TodoCategoryAdded(id, categoryId, _now.AddMinutes(1), "editor"), 2));
        await _projection.ApplyAsync(
            Stored(new TodoListChanged(id, originalListId, _now.AddMinutes(2), "editor"), 3));
        await _projection.ApplyAsync(
            Stored(
                new TodoStatusChanged(
                    id,
                    tenantId,
                    TodoStatus.Pending,
                    TodoStatus.Completed,
                    _now.AddMinutes(3),
                    "editor"),
                4));
        await _projection.ApplyAsync(
            Stored(new TodoDeleted(id, false, _now.AddMinutes(4), "deleter"), 5));

        await _projection.ApplyAsync(
            Stored(
                new TodoCreated(
                    id,
                    tenantId,
                    "Recreated report",
                    "Fresh start",
                    TodoPriority.Low,
                    _now.AddDays(3),
                    newListId,
                    _now.AddDays(1),
                    "recreator"),
                6));

        TodoReadModel model = await ReadPersistedAsync();
        model.Title.Should().Be("Recreated report");
        model.Description.Should().Be("Fresh start");
        model.Priority.Should().Be(TodoPriority.Low);
        model.DueAtUtc.Should().Be(_now.AddDays(3));
        model.ListId.Should().Be(newListId);
        model.Status.Should().Be(TodoStatus.Pending);
        model.CompletedAtUtc.Should().BeNull();
        model.CategoryIds.Should().BeEmpty();
        model.CreatedAtUtc.Should().Be(_now.AddDays(1));
        model.CreatedBy.Should().Be("recreator");
        model.ModifiedAtUtc.Should().BeNull();
        model.ModifiedBy.Should().BeNull();
        model.IsDeleted.Should().BeFalse();
        model.DeletedAtUtc.Should().BeNull();
        model.DeletedBy.Should().BeNull();
        model.Version.Should().Be(6);
    }

    [Fact]
    public async Task Apply_WithUnknownEvent_ShouldFailWithProjectionApplyCode()
    {
        StoredEvent stored = new(
            "todo:unknown",
            1,
            Guid.NewGuid(),
            "todo.unknown",
            1,
            "{}",
            new DefaultMetadata(),
            _now,
            1);

        Result result = await _projection.ApplyAsync(stored);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should()
            .ContainSingle()
            .Which.Code.Should().Be(EventSourcingOutcome.Codes.ProjectionApplyFailed);
        _context.ChangeTracker.Clear();
        (await _context.Todos.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Apply_WithAnUnrelatedEvent_ShouldBeANoOp()
    {
        string name = _projection.Name;

        Result result = await _projection.ApplyAsync(
            Stored(new ReminderScheduled(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _now), 1));

        name.Should().Be("todos");
        result.IsSuccess.Should().BeTrue();
        _context.ChangeTracker.Clear();
        (await _context.Todos.CountAsync()).Should().Be(0);
    }

    private async Task CreateAsync(Guid id, Guid tenantId)
        => await _projection.ApplyAsync(
            Stored(
                new TodoCreated(
                    id,
                    tenantId,
                    "Write report",
                    "Draft the quarterly report",
                    TodoPriority.High,
                    _now.AddDays(1),
                    null,
                    _now,
                    "creator"),
                1));

    private async Task<TodoReadModel> ReadPersistedAsync()
    {
        // Verify: clear the tracker so assertions read the database instead of in-memory mutations.
        _context.ChangeTracker.Clear();
        return await _context.Todos.SingleAsync();
    }

    private StoredEvent Stored(IDomainEvent domainEvent, long version)
        => new(
            $"todo:{domainEvent.GetType().Name}",
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
        public DbSet<TodoReadModel> Todos => Set<TodoReadModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TodoReadModelConfiguration());
            modelBuilder.Ignore<TodoList>();
            modelBuilder.Ignore<TodoCategory>();
        }
    }
}
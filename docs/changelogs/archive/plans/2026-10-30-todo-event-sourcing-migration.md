# Todo Event-Sourcing Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn `Todo` into a fully event-sourced aggregate whose `todos` table is a rebuildable projection, without changing any public Todo API contract.

**Architecture:** Core owns the event-sourced `Todo` aggregate, its self-contained events, validation and snapshots. Application writes through `IEventSourcedRepository<Todo, Guid>` and reads `TodoReadModel`. Infrastructure projects the event log into the existing `todos` table and provides an idempotent backfill for rows created before the migration.

**Tech Stack:** .NET 10, EF Core 10/PostgreSQL 16, `System.Text.Json`, MediatR, xUnit v3, AwesomeAssertions, Testcontainers.PostgreSql.

**Spec:** `docs/specs/2026-10-29-event-sourcing-design.md` (§8 and the wave-1 contracts it builds on)

## Global Constraints

- Follow `docs/specs/2026-10-30-monadic-error-handling-design.md`: flow failures return `Result`; throws are boundary-only and carry `// Boundary:`.
- `Core` has zero project references; `Application` references `Core` only. No Npgsql type appears in `Core` or `Application`.
- No public Todo API contract changes: routes, request bodies, response models, status codes and error codes stay identical.
- Stream version is the event count; the row `Version` is an idempotency marker for projections. `TodoReadModel` must not implement `IVersionable`.
- Event-store append, outbox staging and aggregate state changes commit together. `SaveAsync` clears uncommitted events only after the commit.
- Projections are at-least-once and idempotent: upsert keyed by id with `incomingVersion > storedVersion`.
- Dotted-role filenames, labeled comments, `make structure.check`, `make lib.format.check`.
- Coverage floors 90% line / 80% branch for Core, Application, Infrastructure; `make ci` green after every task.
- Tests: xUnit v3 with MTP. Use `dotnet test --project … -c Release --no-build --filter-class …`. A filter matching zero tests exits `8`.
- Migration ordering: `AddEventStore` is already applied; Todo read-model schema changes must not drop the `todos` table or its data.

## File Structure

| Area | Files |
|---|---|
| Core events | `src/Core/Todos/Items/Todo.Event.cs` (replace) |
| Core aggregate | `src/Core/Todos/Items/Todo.cs`, `Todo.Extension.cs`, `Todo.Interface.cs` |
| Core snapshot | `src/Core/Todos/Items/Todo.Snapshot.cs`, `Todo.Factory.cs` |
| Core validation | `src/Core/Todos/Items/Todo.Validator.cs` |
| Application read model | `src/Application/Features/Todos/Items/Models/Todo.ReadModel.cs` |
| Application mapping | `src/Application/Features/Todos/Items/Mappings/Todo.Mapper.cs` |
| Application commands | `src/Application/Features/Todos/Items/Commands/*.cs` |
| Application queries | `src/Application/Features/Todos/Items/Queries/*.cs` |
| Application bridge | `src/Application/Features/Todos/Events/TodoStatusChanged.Integration.Handler.cs` |
| Infrastructure projection | `src/Infrastructure/Persistence/EntityFramework/Projections/Todo.Projection.cs` |
| Infrastructure EF | `src/Infrastructure/Persistence/Schemas/Todos/EntityConfiguration/TodoReadModel.Configuration.cs` (delete `TodoEntityTypeConfiguration.cs`) |
| Infrastructure query map | `src/Infrastructure/Persistence/Schemas/Todos/Queries/TodoReadModel.Query.cs` (delete `Todo.Query.cs`) |
| Infrastructure cleanup | `src/Infrastructure/Services/Todos/TodoReferenceCleaner.cs` |
| Infrastructure backfill | `src/Infrastructure/Persistence/Schemas/Todos/Seeders/TodoBackfill.Seeder.cs` |
| Composition | `EventStoreSetting`, `EventStore.Composition.cs`, `Messaging.Composition.cs` |
| Tests | Items/Reminders/EventSourcing test folders listed per task |

---

### Task 1: Event-Sourced Todo Domain

**Files:**
- Modify: `src/Core/Todos/Items/Todo.Event.cs` (replace every event)
- Modify: `src/Core/Todos/Items/Todo.cs` (replace aggregate)
- Modify: `src/Core/Todos/Items/Todo.Extension.cs` (replace mutators)
- Modify: `src/Core/Todos/Items/Todo.Interface.cs` (get-only state)
- Modify: `src/Core/Todos/Items/Todo.Validator.cs` (restore/update helpers)
- Create: `src/Core/Todos/Items/Todo.Snapshot.cs`
- Create: `src/Core/Todos/Items/Todo.Factory.cs`
- Test: `tests/Unit/Core.Unit.Tests/Todos/Items/Todo.EventSourced.Spec.cs`
- Modify: `tests/Unit/Core.Unit.Tests/Todos/Items/Todo.Spec.cs`
- Modify: `tests/Unit/Core.Unit.Tests/Todos/Items/Todo.Extension.Spec.cs`
- Modify: `tests/Unit/Core.Unit.Tests/Todos/Items/Todo.Event.Spec.cs`

**Interfaces:**
- Consumes: `EventSourcedAggregate<Guid>`, `ISnapshotable`, `IAggregateFactory<Todo>`, `TodoValidator`, `TodoStatusExtensions`.
- Produces: `Todo.Create`, `Todo.Update`, `Todo.Start/Complete/Cancel/Reopen`, `Todo.AssignToList/ClearList`, `Todo.AddCategory/RemoveCategory`, `Todo.Delete(bool isHard, …)`, `Todo.Restore`, `Todo.CaptureSnapshot/RestoreSnapshot`, `TodoFactory`, and the events `TodoCreated`, `TodoUpdated`, `TodoStatusChanged`, `TodoCategoryAdded`, `TodoCategoryRemoved`, `TodoListChanged`, `TodoDeleted`, `TodoRestored`. Every mutator returns `Result` and raises exactly one event except `Create` (one) and `Update` (one).
- Hard deletion is aggregate state, not just an event flag: `TodoDeleted.IsHard` sets `IsHardDeleted`, the snapshot carries `IsHardDeleted`, `TodoRestored` clears it, and `Restore` fails with `TodoOutcome.Failure.HardDeletedNotFound` (`Todo.Deleted.Hard`, 404, "The todo was permanently deleted.") on a hard-deleted stream. `ClearList`/`RemoveCategory` take an `allowDeleted` escape hatch so reference cleanup can touch soft-deleted streams while user mutators stay blocked.

- [ ] **Step 1: Write the failing domain tests**

Create `tests/Unit/Core.Unit.Tests/Todos/Items/Todo.EventSourced.Spec.cs`:

```csharp
using Core.Todos.Items;

namespace Core.Unit.Tests.Todos.Items;

/// <summary>Rehydration, snapshot and transition tests for the event-sourced todo.</summary>
[Trait("Category", "Unit")]
public sealed class TodoEventSourcedSpec
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_ShouldRaiseTodoCreatedWithFullState()
    {
        Result<Todo> result = Todo.Create(
            Guid.NewGuid(), TenantId, "  Write tests  ", Now, "alice",
            description: "  details  ", priority: TodoPriority.High,
            dueAtUtc: Now.AddDays(1), listId: Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        Todo todo = result.Value;
        todo.Title.Should().Be("Write tests");
        todo.Description.Should().Be("details");
        todo.Status.Should().Be(TodoStatus.Pending);
        todo.Version.Should().Be(1);

        TodoCreated created = todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoCreated>().Subject;
        created.TodoId.Should().Be(todo.Id);
        created.TenantId.Should().Be(TenantId);
        created.CreatedAtUtc.Should().Be(Now);
        created.CreatedBy.Should().Be("alice");
    }

    [Fact]
    public void Update_ShouldRaiseOneEventWithFinalState()
    {
        Todo todo = Create();
        Guid listId = Guid.NewGuid();

        Result result = todo.Update(
            "  Renamed  ", "desc", TodoPriority.Urgent,
            Now.AddDays(2), listId, Now.AddMinutes(5), "bob");

        result.IsSuccess.Should().BeTrue();
        todo.Title.Should().Be("Renamed");
        todo.Description.Should().Be("desc");
        todo.Priority.Should().Be(TodoPriority.Urgent);
        todo.DueAtUtc.Should().Be(Now.AddDays(2));
        todo.ListId.Should().Be(listId);
        todo.ModifiedBy.Should().Be("bob");
        todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoUpdated>();
    }

    [Fact]
    public void StatusTransitions_ShouldRaiseStatusChanged()
    {
        Todo todo = Create();

        todo.Start(Now.AddMinutes(1), "alice").IsSuccess.Should().BeTrue();
        todo.Complete(Now.AddMinutes(2), "alice").IsSuccess.Should().BeTrue();

        todo.Status.Should().Be(TodoStatus.Completed);
        todo.CompletedAtUtc.Should().Be(Now.AddMinutes(2));
        todo.UncommittedEvents.OfType<TodoStatusChanged>().Should().HaveCount(2);
        todo.UncommittedEvents.OfType<TodoStatusChanged>().Last()
            .Should().Match<TodoStatusChanged>(changed =>
                changed.From == TodoStatus.InProgress &&
                changed.To == TodoStatus.Completed);

        todo.Reopen(Now.AddMinutes(3), "alice").IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.Pending);
        todo.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IllegalTransition_ShouldNotMutateOrRaise()
    {
        Todo todo = Create();

        Result result = todo.Complete(Now.AddMinutes(1), "alice");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoOutcome.Failure.StatusTransitionInvalid.Code);
        todo.Version.Should().Be(1);
        todo.UncommittedEvents.Should().ContainSingle();
    }

    [Fact]
    public void CategoriesAndList_ShouldRaiseDedicatedEvents()
    {
        Todo todo = Create();
        Guid categoryId = Guid.NewGuid();
        Guid listId = Guid.NewGuid();

        todo.AddCategory(categoryId, Now.AddMinutes(1), "alice").IsSuccess.Should().BeTrue();
        todo.AssignToList(listId, Now.AddMinutes(2), "alice").IsSuccess.Should().BeTrue();
        todo.ClearList(Now.AddMinutes(3), "alice").IsSuccess.Should().BeTrue();
        todo.RemoveCategory(categoryId, Now.AddMinutes(4), "alice").IsSuccess.Should().BeTrue();

        todo.CategoryIds.Should().BeEmpty();
        todo.ListId.Should().BeNull();
        todo.UncommittedEvents.Select(domainEvent => domainEvent.GetType().Name)
            .Should().ContainInOrder(
                nameof(TodoCreated), nameof(TodoCategoryAdded), nameof(TodoListChanged),
                nameof(TodoListChanged), nameof(TodoCategoryRemoved));
    }

    [Fact]
    public void DeleteAndRestore_ShouldRaiseLifecycleEvents()
    {
        Todo todo = Create();

        todo.Delete(isHard: false, Now.AddMinutes(1), "alice").IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeTrue();
        todo.DeletedBy.Should().Be("alice");

        todo.Restore(Now.AddMinutes(2), "alice").IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeFalse();
        todo.DeletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Snapshot_ShouldRoundTrip()
    {
        Todo todo = Create();
        todo.AddCategory(Guid.NewGuid(), Now.AddMinutes(1), "alice");
        todo.Start(Now.AddMinutes(2), "alice");

        string state = todo.CaptureSnapshot();
        Todo restored = new TodoFactory().Create();
        restored.RestoreSnapshot(state);

        restored.Id.Should().Be(todo.Id);
        restored.TenantId.Should().Be(todo.TenantId);
        restored.Title.Should().Be(todo.Title);
        restored.Status.Should().Be(TodoStatus.InProgress);
        restored.CategoryIds.Should().BeEquivalentTo(todo.CategoryIds);
        restored.Version.Should().Be(todo.Version);
    }

    [Fact]
    public void Rehydration_ShouldMatchLiveState()
    {
        Todo live = Create();
        live.AddCategory(Guid.NewGuid(), Now.AddMinutes(1), "alice");
        live.Start(Now.AddMinutes(2), "alice");
        live.Complete(Now.AddMinutes(3), "alice");
        live.Update("Final", "final", TodoPriority.Low, null, null, Now.AddMinutes(4), "bob");

        var rehydrated = new TodoFactory().Create();
        Result loaded = rehydrated.TryLoadFromHistory(
            live.Version,
            live.UncommittedEvents);

        loaded.IsSuccess.Should().BeTrue();
        rehydrated.Id.Should().Be(live.Id);
        rehydrated.Title.Should().Be("Final");
        rehydrated.Status.Should().Be(TodoStatus.Completed);
        rehydrated.CompletedAtUtc.Should().Be(Now.AddMinutes(3));
        rehydrated.ModifiedBy.Should().Be("bob");
        rehydrated.Version.Should().Be(live.Version);
        rehydrated.UncommittedEvents.Should().BeEmpty();
    }

    private static Todo Create() =>
        Todo.Create(Guid.NewGuid(), TenantId, "Original", Now, "alice").Value;
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release --no-build --filter-class "*TodoEventSourcedSpec*"
```

Expected: compile errors for the new methods/events, then a failing run.

- [ ] **Step 3: Replace `Todo.Event.cs`**

```csharp
using System.Text.Json.Serialization;

using Core.Common.Domain.Events;
using Core.Common.Monad.Metadata;

namespace Core.Todos.Items;

/// <summary>Raised when a todo is created.</summary>
public sealed record TodoCreated(
    Guid TodoId,
    Guid TenantId,
    string Title,
    string? Description,
    TodoPriority Priority,
    DateTimeOffset? DueAtUtc,
    Guid? ListId,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a todo's editable fields change.</summary>
public sealed record TodoUpdated(
    Guid TodoId,
    string Title,
    string? Description,
    TodoPriority Priority,
    DateTimeOffset? DueAtUtc,
    Guid? ListId,
    DateTimeOffset ModifiedAtUtc,
    string? ModifiedBy,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised for start, complete, cancel and reopen.</summary>
public sealed record TodoStatusChanged(
    Guid TodoId,
    Guid TenantId,
    TodoStatus From,
    TodoStatus To,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a category reference is added.</summary>
public sealed record TodoCategoryAdded(
    Guid TodoId,
    Guid CategoryId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a category reference is removed.</summary>
public sealed record TodoCategoryRemoved(
    Guid TodoId,
    Guid CategoryId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when the list reference changes; a null list clears it.</summary>
public sealed record TodoListChanged(
    Guid TodoId,
    Guid? ListId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a todo is deleted; hard deletes remove the projection row.</summary>
public sealed record TodoDeleted(
    Guid TodoId,
    bool IsHard,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a soft-deleted todo is restored.</summary>
public sealed record TodoRestored(
    Guid TodoId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);
```

- [ ] **Step 4: Replace `Todo.cs`**

```csharp
using System.Text.Json;

using Core.Common.Domain.EventSourcing;
using Core.Common.Domain.Events;
using Core.Common.Monad.Metadata;

namespace Core.Todos.Items;

/// <summary>An event-sourced unit of work with a lifecycle, a priority, and optional references.</summary>
public sealed class Todo : EventSourcedAggregate<Guid>, ITodo, ISnapshotable
{
    private readonly List<Guid> _categoryIds = [];

    internal Todo()
    {
        Title = string.Empty;
        Status = TodoConstant.Defaults.Status;
        Priority = TodoConstant.Defaults.Priority;
    }

    /// <inheritdoc />
    public string Title { get; private set; }

    /// <inheritdoc />
    public string? Description { get; private set; }

    /// <inheritdoc />
    public TodoStatus Status { get; private set; }

    /// <inheritdoc />
    public TodoPriority Priority { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DueAtUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <inheritdoc />
    public Guid? ListId { get; private set; }

    /// <inheritdoc />
    public ICollection<Guid> CategoryIds => _categoryIds;

    /// <inheritdoc />
    public bool IsCompleted => Status == TodoStatus.Completed;

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <inheritdoc />
    public string? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; private set; }

    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    /// <inheritdoc />
    public string? DeletedBy { get; private set; }

    /// <inheritdoc />
    public Guid TenantId { get; private set; }

    /// <summary>Creates a pending todo and records its creation event.</summary>
    public static Result<Todo> Create(
        Guid id,
        Guid tenantId,
        string? title,
        DateTimeOffset nowUtc,
        string? actor = null,
        string? description = null,
        TodoPriority priority = TodoConstant.Defaults.Priority,
        DateTimeOffset? dueAtUtc = null,
        Guid? listId = null,
        IMetadata? metadata = null)
    {
        var candidate = new Todo
        {
            Id = id,
            TenantId = tenantId,
            Title = title?.Trim() ?? string.Empty,
            Description = description?.Trim(),
            Priority = priority,
            DueAtUtc = dueAtUtc?.ToUniversalTime(),
            ListId = listId,
            CreatedAtUtc = nowUtc.ToUniversalTime(),
            CreatedBy = actor?.Trim(),
            Status = TodoConstant.Defaults.Status,
        };

        Result<Todo> validated = TodoValidator.Validate(candidate);
        if (validated.IsFailure)
            return validated;

        DefaultMetadata? eventMetadata = metadata is null
            ? null
            : new DefaultMetadata(metadata.ToDictionary(pair => pair.Key, pair => pair.Value));
        candidate.Raise(new TodoCreated(
            candidate.Id,
            candidate.TenantId,
            candidate.Title,
            candidate.Description,
            candidate.Priority,
            candidate.DueAtUtc,
            candidate.ListId,
            candidate.CreatedAtUtc,
            candidate.CreatedBy,
            eventMetadata));

        return Result<Todo>.Success(candidate);
    }

    /// <summary>Replaces the editable fields in one event.</summary>
    public Result Update(
        string? title,
        string? description,
        TodoPriority priority,
        DateTimeOffset? dueAtUtc,
        Guid? listId,
        DateTimeOffset nowUtc,
        string? actor)
    {
        // Guard: a deleted todo is immutable until restored.
        if (IsDeleted)
            return Result.Fail(TodoOutcome.Failure.DeletedNotMutable);

        var normalizedTitle = title?.Trim() ?? string.Empty;
        var normalizedDescription = description?.Trim();
        var normalizedDue = dueAtUtc?.ToUniversalTime();

        Result validation = TodoValidator.ValidateTitle(normalizedTitle);
        if (validation.IsFailure)
            return validation;

        validation = TodoValidator.ValidateDescription(normalizedDescription);
        if (validation.IsFailure)
            return validation;

        validation = TodoValidator.ValidatePriority(priority);
        if (validation.IsFailure)
            return validation;

        validation = TodoValidator.ValidateDueDate(normalizedDue, CreatedAtUtc);
        if (validation.IsFailure)
            return validation;

        validation = TodoValidator.ValidateListId(listId);
        if (validation.IsFailure)
            return validation;

        Raise(new TodoUpdated(
            Id,
            normalizedTitle,
            normalizedDescription,
            priority,
            normalizedDue,
            listId,
            nowUtc.ToUniversalTime(),
            actor?.Trim()));

        return Result.Ok();
    }

    /// <summary>Moves a pending todo into progress.</summary>
    public Result Start(DateTimeOffset nowUtc, string? actor)
        => Transition(TodoStatus.InProgress, nowUtc, actor);

    /// <summary>Completes a started todo.</summary>
    public Result Complete(DateTimeOffset nowUtc, string? actor)
        => Transition(TodoStatus.Completed, nowUtc, actor);

    /// <summary>Cancels a pending or started todo.</summary>
    public Result Cancel(DateTimeOffset nowUtc, string? actor)
        => Transition(TodoStatus.Cancelled, nowUtc, actor);

    /// <summary>Reopens a finished todo.</summary>
    public Result Reopen(DateTimeOffset nowUtc, string? actor)
        => Transition(TodoStatus.Pending, nowUtc, actor);

    /// <summary>Assigns the todo to a list.</summary>
    public Result AssignToList(Guid listId, DateTimeOffset nowUtc, string? actor)
    {
        Result validation = TodoValidator.ValidateNotDeleted(IsDeleted);
        if (validation.IsFailure)
            return validation;

        validation = TodoValidator.ValidateListId(listId);
        if (validation.IsFailure)
            return validation;

        Raise(new TodoListChanged(Id, listId, nowUtc.ToUniversalTime(), actor?.Trim()));
        return Result.Ok();
    }

    /// <summary>Clears the list reference.</summary>
    public Result ClearList(DateTimeOffset nowUtc, string? actor)
    {
        Result validation = TodoValidator.ValidateNotDeleted(IsDeleted);
        if (validation.IsFailure)
            return validation;

        Raise(new TodoListChanged(Id, null, nowUtc.ToUniversalTime(), actor?.Trim()));
        return Result.Ok();
    }

    /// <summary>Adds a category reference.</summary>
    public Result AddCategory(Guid categoryId, DateTimeOffset nowUtc, string? actor)
    {
        Result validation = TodoValidator.ValidateNotDeleted(IsDeleted);
        if (validation.IsFailure)
            return validation;

        validation = TodoValidator.ValidateCategoryIds([.. _categoryIds, categoryId]);
        if (validation.IsFailure)
            return validation;

        Raise(new TodoCategoryAdded(Id, categoryId, nowUtc.ToUniversalTime(), actor?.Trim()));
        return Result.Ok();
    }

    /// <summary>Removes a category reference.</summary>
    public Result RemoveCategory(Guid categoryId, DateTimeOffset nowUtc, string? actor)
    {
        Result validation = TodoValidator.ValidateNotDeleted(IsDeleted);
        if (validation.IsFailure)
            return validation;

        if (!_categoryIds.Contains(categoryId))
            return Result.Fail(TodoOutcome.Failure.CategoryNotFound);

        Raise(new TodoCategoryRemoved(Id, categoryId, nowUtc.ToUniversalTime(), actor?.Trim()));
        return Result.Ok();
    }

    /// <summary>Soft- or hard-deletes the todo. A hard delete removes the projection row.</summary>
    public Result Delete(bool isHard, DateTimeOffset nowUtc, string? actor)
    {
        // Guard: a soft-deleted todo stays deleted without raising another event.
        if (IsDeleted && !isHard)
            return Result.Ok();

        Raise(new TodoDeleted(Id, isHard, nowUtc.ToUniversalTime(), actor?.Trim()));
        return Result.Ok();
    }

    /// <summary>Restores a soft-deleted todo.</summary>
    public Result Restore(DateTimeOffset nowUtc, string? actor)
    {
        // Guard: a live todo has nothing to restore.
        if (!IsDeleted)
            return Result.Ok();

        Raise(new TodoRestored(Id, nowUtc.ToUniversalTime(), actor?.Trim()));
        return Result.Ok();
    }

    /// <inheritdoc />
    public string CaptureSnapshot()
        => JsonSerializer.Serialize(new TodoSnapshot(
            Id,
            TenantId,
            Title,
            Description,
            Status,
            Priority,
            DueAtUtc,
            CompletedAtUtc,
            ListId,
            [.. _categoryIds],
            CreatedAtUtc,
            CreatedBy,
            ModifiedAtUtc,
            ModifiedBy,
            IsDeleted,
            DeletedAtUtc,
            DeletedBy,
            Version));

    /// <inheritdoc />
    public void RestoreSnapshot(string state)
    {
        TodoSnapshot? snapshot = JsonSerializer.Deserialize<TodoSnapshot>(state);

        // Boundary: a corrupt snapshot is a storage invariant failure; the repository falls back to replay.
        if (snapshot is null)
            throw new InvalidOperationException("The todo snapshot is invalid.");

        Id = snapshot.Id;
        TenantId = snapshot.TenantId;
        Title = snapshot.Title;
        Description = snapshot.Description;
        Status = snapshot.Status;
        Priority = snapshot.Priority;
        DueAtUtc = snapshot.DueAtUtc;
        CompletedAtUtc = snapshot.CompletedAtUtc;
        ListId = snapshot.ListId;
        _categoryIds.Clear();
        _categoryIds.AddRange(snapshot.CategoryIds);
        CreatedAtUtc = snapshot.CreatedAtUtc;
        CreatedBy = snapshot.CreatedBy;
        ModifiedAtUtc = snapshot.ModifiedAtUtc;
        ModifiedBy = snapshot.ModifiedBy;
        IsDeleted = snapshot.IsDeleted;
        DeletedAtUtc = snapshot.DeletedAtUtc;
        DeletedBy = snapshot.DeletedBy;
    }

    /// <inheritdoc />
    protected override void When(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case TodoCreated created:
                Id = created.TodoId;
                TenantId = created.TenantId;
                Title = created.Title;
                Description = created.Description;
                Status = TodoConstant.Defaults.Status;
                Priority = created.Priority;
                DueAtUtc = created.DueAtUtc;
                ListId = created.ListId;
                CreatedAtUtc = created.CreatedAtUtc;
                CreatedBy = created.CreatedBy;
                IsDeleted = false;
                _categoryIds.Clear();
                break;

            case TodoUpdated updated:
                Title = updated.Title;
                Description = updated.Description;
                Priority = updated.Priority;
                DueAtUtc = updated.DueAtUtc;
                ListId = updated.ListId;
                ModifiedAtUtc = updated.ModifiedAtUtc;
                ModifiedBy = updated.ModifiedBy;
                break;

            case TodoStatusChanged changed:
                Status = changed.To;
                CompletedAtUtc = changed.To == TodoStatus.Completed ? changed.OccurredAtUtc : null;
                ModifiedAtUtc = changed.OccurredAtUtc;
                ModifiedBy = changed.Actor;
                break;

            case TodoCategoryAdded added:
                if (!_categoryIds.Contains(added.CategoryId))
                    _categoryIds.Add(added.CategoryId);
                ModifiedAtUtc = added.OccurredAtUtc;
                ModifiedBy = added.Actor;
                break;

            case TodoCategoryRemoved removed:
                _categoryIds.Remove(removed.CategoryId);
                ModifiedAtUtc = removed.OccurredAtUtc;
                ModifiedBy = removed.Actor;
                break;

            case TodoListChanged changedList:
                ListId = changedList.ListId;
                ModifiedAtUtc = changedList.OccurredAtUtc;
                ModifiedBy = changedList.Actor;
                break;

            case TodoDeleted deleted:
                IsDeleted = true;
                DeletedAtUtc = deleted.OccurredAtUtc;
                DeletedBy = deleted.Actor;
                break;

            case TodoRestored restored:
                IsDeleted = false;
                DeletedAtUtc = null;
                DeletedBy = null;
                ModifiedAtUtc = restored.OccurredAtUtc;
                ModifiedBy = restored.Actor;
                break;

            default:
                throw new InvalidOperationException(
                    $"Event '{domainEvent.GetType().Name}' is not supported by Todo.");
        }
    }

    private Result Transition(TodoStatus next, DateTimeOffset nowUtc, string? actor)
    {
        Result validation = TodoValidator.ValidateNotDeleted(IsDeleted);
        if (validation.IsFailure)
            return validation;

        validation = TodoValidator.ValidateTransition(Status, next);
        if (validation.IsFailure)
            return validation;

        Raise(new TodoStatusChanged(
            Id,
            TenantId,
            Status,
            next,
            nowUtc.ToUniversalTime(),
            actor?.Trim()));

        return Result.Ok();
    }
}
```

- [ ] **Step 5: Add snapshot and factory**

`Todo.Snapshot.cs`:

```csharp
namespace Core.Todos.Items;

/// <summary>Serializable snapshot shape for <see cref="Todo"/>.</summary>
public sealed record TodoSnapshot(
    Guid Id,
    Guid TenantId,
    string Title,
    string? Description,
    TodoStatus Status,
    TodoPriority Priority,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset? CompletedAtUtc,
    Guid? ListId,
    IReadOnlyList<Guid> CategoryIds,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? ModifiedAtUtc,
    string? ModifiedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy,
    long Version);
```

`Todo.Factory.cs`:

```csharp
using Core.Common.Domain.EventSourcing;

namespace Core.Todos.Items;

/// <summary>Creates an empty todo for event-store rehydration.</summary>
public sealed class TodoFactory : IAggregateFactory<Todo>
{
    /// <inheritdoc />
    public Todo Create() => new();
}
```

- [ ] **Step 6: Make `ITodo` get-only**

In `Todo.Interface.cs`, change every state property from `{ get; set; }` to `{ get; }`. Keep `ICollection<Guid> CategoryIds { get; }`.

- [ ] **Step 7: Replace `Todo.Extension.cs`**

Delete the file. Its mutators now live on the aggregate (`Update`, `Start`, `Complete`, `Cancel`, `Reopen`, `AssignToList`, `ClearList`, `AddCategory`, `RemoveCategory`) and the extension surface is no longer needed. Any remaining helpers must not duplicate aggregate logic.

- [ ] **Step 8: Extend the validator**

Add to `TodoValidator`:

```csharp
/// <summary>Validates that a todo is deleted before it is restored.</summary>
public static Result ValidateDeleted(bool isDeleted)
    => isDeleted ? Result.Success() : Result.Fail(TodoOutcome.Failure.DeletedNotMutable);
```

Also update `Validate` so `CreatedAtUtc == default` is rejected only for persisted/mutating calls, not for the empty factory. Keep the current `ValidateDueDate` behavior.

- [ ] **Step 9: Update the existing core tests**

- `Todo.Spec.cs`: replace constructor-based assertions with `Todo.Create` and event assertions.
- `Todo.Event.Spec.cs`: assert the new record payloads, including `TodoStatusChanged` from/to/occurred and `TodoDeleted.IsHard`.
- `Todo.Extension.Spec.cs`: delete it; its coverage moved to `Todo.EventSourced.Spec.cs` and `Todo.Spec.cs`.
- `Todo.Validator.Spec.cs`: add `ValidateDeleted` cases.

- [ ] **Step 10: Run the core tests**

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release --no-build
```

Expected: all Core tests pass.

- [ ] **Step 11: Commit**

```bash
git add src/Core/Todos/Items tests/Unit/Core.Unit.Tests/Todos/Items
git commit -m "feat(core): make Todo an event-sourced aggregate"
```

---

### Task 2: Todo Read Model and Projection

**Files:**
- Create: `src/Application/Features/Todos/Items/Models/Todo.ReadModel.cs`
- Modify: `src/Application/Features/Todos/Items/Mappings/Todo.Mapper.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Projections/Todo.Projection.cs`
- Create: `src/Infrastructure/Persistence/Schemas/Todos/EntityConfiguration/TodoReadModel.Configuration.cs`
- Delete: `src/Infrastructure/Persistence/Schemas/Todos/EntityConfiguration/TodoEntityTypeConfiguration.cs`
- Create: `src/Infrastructure/Persistence/Schemas/Todos/Queries/TodoReadModel.Query.cs`
- Delete: `src/Infrastructure/Persistence/Schemas/Todos/Queries/Todo.Query.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Projections/Todo.Projection.Test.cs`
- Test: `tests/Unit/Application.Unit.Tests/Features/Todos/Items/Mappings/TodoReadModel.Mapper.Test.cs`

**Interfaces:**
- Consumes: Task 1 events; `IProjection`, `StoredEvent`, `IDomainEventSerializer`, `DbContext`.
- Produces: `TodoReadModel` (with `IEntity<Guid>`, `ITenantable<Guid>`, `ISoftDeletable`), `TodoProjection` named `"todos"`, `TodoReadModel` query map keyed by the same field names as the old `Todo` map.

- [ ] **Step 1: Write the failing projection test**

Create `Todo.Projection.Test.cs` with an in-memory SQLite context, a `JsonMessageSerializer` and the same `Stored(...)` helper as the reminder projection test. Cover:
1. `TodoCreated` inserts a pending row.
2. `TodoUpdated`, `TodoStatusChanged`, `TodoCategoryAdded`, `TodoListChanged` mutate the row.
3. `TodoDeleted` soft-deletes; `TodoDeleted(IsHard: true)` removes the row; `TodoRestored` clears the flags.
4. Replaying a version lower than the stored `Version` is a no-op.
5. An unrelated event is a no-op and `Name` is `"todos"`.

Use this fixture body:

```csharp
private sealed class ProjectionTestContext(DbContextOptions<ProjectionTestContext> options)
    : DbContext(options)
{
    public DbSet<TodoReadModel> Todos => Set<TodoReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TodoReadModelConfiguration());
        modelBuilder.Ignore<TodoList>();
        modelBuilder.Ignore<TodoCategory>();
    }
}
```

- [ ] **Step 2: Run it and confirm failure**

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
```

Expected: `TodoReadModel` and `TodoProjection` do not exist.

- [ ] **Step 3: Add `TodoReadModel`**

```csharp
using Core.Common.Concerns.Foundation.Tenantable;
using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Common.Domain.Entities;
using Core.Todos.Items;

namespace Application.Features.Todos.Items.Models;

/// <summary>Relational projection of the Todo event stream.</summary>
public sealed record TodoReadModel : IEntity<Guid>, ITenantable<Guid>, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TodoStatus Status { get; set; }
    public TodoPriority Priority { get; set; }
    public DateTimeOffset? DueAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public Guid? ListId { get; set; }
    public List<Guid> CategoryIds { get; set; } = [];
    public bool IsCompleted => Status == TodoStatus.Completed;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public long Version { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = [];
}
```

- [ ] **Step 4: Add the mapper overload**

In `TodoMapper`, keep `ToModel<T>(this Todo)` and `ToSummary<T>(this Todo)` for command responses; add identical overloads for `TodoReadModel`. `ToSummary` copies `Id`, `Title`, `Status`, `Priority`, `DueAtUtc`, `ListId`, `CategoryIds`, `IsCompleted`, `CreatedAtUtc`.

- [ ] **Step 5: Add `TodoReadModelConfiguration`**

Copy the column mapping from the deleted `TodoEntityTypeConfiguration`, replacing `Todo` with `TodoReadModel`, and:
- keep `builder.ToTable("todos")` (default schema) and every column name;
- keep `HasGuidListConversion()` on `CategoryIds`;
- keep `HasMetadataConversion()` on `Metadata`;
- map `Version` with `.HasColumnName("version")` and do **not** call `IsConcurrencyToken()`;
- map `IsCompleted` with `.Ignore()`;
- keep every index;
- delete `TodoEntityTypeConfiguration.cs`.

- [ ] **Step 6: Add `TodoProjection`**

Follow the reminder projection shape:

```csharp
using Application.Common.EventSourcing;
using Application.Common.Messaging.Serialization;
using Application.Features.Todos.Items.Models;

using Core.Common.Domain.Events;
using Core.Todos.Items;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Projections;

/// <summary>Projects todo events into the todos read model.</summary>
[ExcludeFromCodeCoverage]
public sealed class TodoProjection(
    DbContext context,
    IDomainEventSerializer serializer) : IProjection
{
    /// <inheritdoc />
    public string Name => "todos";

    /// <inheritdoc />
    public async Task<Result> ApplyAsync(
        StoredEvent storedEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDomainEvent domainEvent = serializer.Deserialize(
                storedEvent.EventName,
                storedEvent.EventVersion,
                storedEvent.Payload);

            switch (domainEvent)
            {
                case TodoCreated created:
                    await ApplyCreatedAsync(created, storedEvent.Version, cancellationToken);
                    break;
                case TodoUpdated updated:
                    await ApplyUpdatedAsync(updated, storedEvent.Version, cancellationToken);
                    break;
                case TodoStatusChanged changed:
                    await ApplyStatusAsync(changed, storedEvent.Version, cancellationToken);
                    break;
                case TodoCategoryAdded added:
                    await ApplyCategoryAsync(added.TodoId, added.CategoryId, added.OccurredAtUtc, added.Actor, true, storedEvent.Version, cancellationToken);
                    break;
                case TodoCategoryRemoved removed:
                    await ApplyCategoryAsync(removed.TodoId, removed.CategoryId, removed.OccurredAtUtc, removed.Actor, false, storedEvent.Version, cancellationToken);
                    break;
                case TodoListChanged listChanged:
                    await ApplyListAsync(listChanged, storedEvent.Version, cancellationToken);
                    break;
                case TodoDeleted deleted:
                    await ApplyDeletedAsync(deleted, storedEvent.Version, cancellationToken);
                    break;
                case TodoRestored restored:
                    await ApplyRestoredAsync(restored, storedEvent.Version, cancellationToken);
                    break;
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result.Fail(
                EventSourcingOutcome.Failure.ProjectionApplyFailed(Name, exception.Message));
        }
    }

    private async Task ApplyCreatedAsync(
        TodoCreated created,
        long version,
        CancellationToken cancellationToken)
    {
        TodoReadModel? model = await context.Set<TodoReadModel>()
            .FindAsync([created.TodoId], cancellationToken);

        if (model is null)
        {
            context.Set<TodoReadModel>().Add(new TodoReadModel
            {
                Id = created.TodoId,
                TenantId = created.TenantId,
                Title = created.Title,
                Description = created.Description,
                Status = TodoStatus.Pending,
                Priority = created.Priority,
                DueAtUtc = created.DueAtUtc,
                ListId = created.ListId,
                CreatedAtUtc = created.CreatedAtUtc,
                CreatedBy = created.CreatedBy,
                Version = version,
            });
            return;
        }

        if (model.Version >= version)
            return;

        model.TenantId = created.TenantId;
        model.Title = created.Title;
        model.Description = created.Description;
        model.Status = TodoStatus.Pending;
        model.Priority = created.Priority;
        model.DueAtUtc = created.DueAtUtc;
        model.ListId = created.ListId;
        model.CategoryIds.Clear();
        model.CompletedAtUtc = null;
        model.CreatedAtUtc = created.CreatedAtUtc;
        model.CreatedBy = created.CreatedBy;
        model.IsDeleted = false;
        model.DeletedAtUtc = null;
        model.DeletedBy = null;
        model.Metadata.Clear();
        model.Version = version;
    }

    private async Task ApplyUpdatedAsync(
        TodoUpdated updated,
        long version,
        CancellationToken cancellationToken)
    {
        TodoReadModel? model = await FindNewerAsync(updated.TodoId, version, cancellationToken);
        if (model is null)
            return;

        model.Title = updated.Title;
        model.Description = updated.Description;
        model.Priority = updated.Priority;
        model.DueAtUtc = updated.DueAtUtc;
        model.ListId = updated.ListId;
        model.ModifiedAtUtc = updated.ModifiedAtUtc;
        model.ModifiedBy = updated.ModifiedBy;
        model.Version = version;
    }

    private async Task ApplyStatusAsync(
        TodoStatusChanged changed,
        long version,
        CancellationToken cancellationToken)
    {
        TodoReadModel? model = await FindNewerAsync(changed.TodoId, version, cancellationToken);
        if (model is null)
            return;

        model.Status = changed.To;
        model.CompletedAtUtc = changed.To == TodoStatus.Completed
            ? changed.OccurredAtUtc
            : null;
        model.ModifiedAtUtc = changed.OccurredAtUtc;
        model.ModifiedBy = changed.Actor;
        model.Version = version;
    }

    private async Task ApplyCategoryAsync(
        Guid todoId,
        Guid categoryId,
        DateTimeOffset occurredAtUtc,
        string? actor,
        bool added,
        long version,
        CancellationToken cancellationToken)
    {
        TodoReadModel? model = await FindNewerAsync(todoId, version, cancellationToken);
        if (model is null)
            return;

        if (added && !model.CategoryIds.Contains(categoryId))
            model.CategoryIds.Add(categoryId);

        if (!added)
            model.CategoryIds.Remove(categoryId);

        model.ModifiedAtUtc = occurredAtUtc;
        model.ModifiedBy = actor;
        model.Version = version;
    }

    private async Task ApplyListAsync(
        TodoListChanged changed,
        long version,
        CancellationToken cancellationToken)
    {
        TodoReadModel? model = await FindNewerAsync(changed.TodoId, version, cancellationToken);
        if (model is null)
            return;

        model.ListId = changed.ListId;
        model.ModifiedAtUtc = changed.OccurredAtUtc;
        model.ModifiedBy = changed.Actor;
        model.Version = version;
    }

    private async Task ApplyDeletedAsync(
        TodoDeleted deleted,
        long version,
        CancellationToken cancellationToken)
    {
        TodoReadModel? model = await FindNewerAsync(deleted.TodoId, version, cancellationToken);
        if (model is null)
            return;

        if (deleted.IsHard)
        {
            // Handle: ExecuteDelete bypasses LifecycleInterceptor, which would rewrite Remove
            // into a soft delete and keep the projection row alive.
            await context.Set<TodoReadModel>()
                .Where(todo => todo.Id == deleted.TodoId)
                .ExecuteDeleteAsync(cancellationToken);

            // Update: the set-based delete bypasses the tracker, so drop the stale tracked row.
            context.Entry(model).State = EntityState.Detached;
            return;
        }

        model.IsDeleted = true;
        model.DeletedAtUtc = deleted.OccurredAtUtc;
        model.DeletedBy = deleted.Actor;
        model.Version = version;
    }

    private async Task ApplyRestoredAsync(
        TodoRestored restored,
        long version,
        CancellationToken cancellationToken)
    {
        TodoReadModel? model = await FindNewerAsync(restored.TodoId, version, cancellationToken);
        if (model is null)
            return;

        model.IsDeleted = false;
        model.DeletedAtUtc = null;
        model.DeletedBy = null;
        model.ModifiedAtUtc = restored.OccurredAtUtc;
        model.ModifiedBy = restored.Actor;
        model.Version = version;
    }

    private async Task<TodoReadModel?> FindNewerAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken)
    {
        TodoReadModel? model = await context.Set<TodoReadModel>()
            .FindAsync([id], cancellationToken);

        return model is not null && model.Version < version ? model : null;
    }
}
```

`[ExcludeFromCodeCoverage]` matches `PostgresEventStore`; the projection is covered by the PostgreSQL flow test in Task 8.

- [ ] **Step 7: Add the read-model query map**

Copy `Todo.Query.cs` to `TodoReadModel.Query.cs`, change the generic to `TodoReadModel`, keep every field name, and rename the definition to `TodoReadModelQueryMapDefinition`. Delete `Todo.Query.cs`.

- [ ] **Step 8: Generate the migration**

```bash
dotnet ef migrations add AddTodoReadModel -p src/Infrastructure -s app/Web.Api -o Persistence/Migrations
```

Expected: no `DropTable("todos")`, no destructive column changes. If EF generates a drop, map the missing column instead of accepting it. Commit the migration and `ApplicationDbContextModelSnapshot.cs`.

- [ ] **Step 9: Run the projection test and the mapper test**

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "*TodoProjectionTest*"
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --filter-class "*TodoReadModelMapperTest*"
```

- [ ] **Step 10: Commit**

```bash
git add src/Application/Features/Todos/Items src/Infrastructure/Persistence tests/Unit
git commit -m "feat(infra): project Todo events into the todos read model"
```

---

### Task 3: Event-Sourced Item Command Handlers

**Files:**
- Create: `src/Application/Features/Todos/Common/TodoEventRepository.Extension.cs`
- Modify: `src/Application/Features/Todos/Items/Commands/CreateTodo.cs`
- Modify: `src/Application/Features/Todos/Items/Commands/UpdateTodo.cs`
- Modify: `src/Application/Features/Todos/Items/Commands/DeleteTodo.cs`
- Modify: `src/Application/Features/Todos/Items/Commands/RestoreTodo.cs`
- Modify: `src/Application/Features/Todos/Items/Commands/StartTodo.cs`
- Modify: `src/Application/Features/Todos/Items/Commands/CompleteTodo.cs`
- Modify: `src/Application/Features/Todos/Items/Commands/CancelTodo.cs`
- Modify: `src/Application/Features/Todos/Items/Commands/ReopenTodo.cs`
- Test: `tests/Unit/Application.Unit.Tests/Features/Todos/Items/Commands/EventSourcedTodo.Commands.Test.cs`

**Interfaces:**
- Consumes: Task 1 aggregate, `IEventSourcedRepository<Todo, Guid>`.
- Produces: a `LoadOwnedAsync` extension returning `Result<Todo>`; command handlers still return the same `Response` models and error codes. `LoadOwnedAsync(id, tenantId, cancellationToken, includeDeleted = false)` hides soft-deleted streams by default, so every mutating command returns `Repository.Entity.NotFound` (404) for them; only `DeleteTodo` and `RestoreTodo` pass `includeDeleted: true`.

- [ ] **Step 1: Write the failing handler tests**

Create `EventSourcedTodo.Commands.Test.cs` with one mock repository and these cases:
- create saves one aggregate with one `TodoCreated` and maps the response;
- create rejects a blank title before save;
- update loads, raises exactly one `TodoUpdated`, and maps final values;
- start/complete/reopen each save one `TodoStatusChanged`;
- delete hard passes `isHard: true` to the event;
- restore raises `TodoRestored`;
- a missing stream returns `Repository.Entity.NotFound`);
- a stream owned by another tenant returns `Repository.Entity.NotFound` and never saves;
- a soft-deleted stream returns `Repository.Entity.NotFound` (404) from `Update` and `Start` and never saves.

Use `Mock<IEventSourcedRepository<Todo, Guid>>` and set up:

```csharp
repository
    .Setup(store => store.LoadAsync(todoId, It.IsAny<CancellationToken>()))
    .ReturnsAsync(Result<Todo>.Success(todo));
repository
    .Setup(store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(Result.Ok());
```

- [ ] **Step 2: Add the ownership extension**

```csharp
using Application.Common.EventSourcing;
using Application.Common.Persistence.Repositories;

using Core.Todos.Items;

namespace Application.Features.Todos.Common;

/// <summary>Tenant-scoped loading for the event-sourced todo stream.</summary>
public static class TodoEventRepositoryExtension
{
    /// <summary>Loads a todo and fails when it is missing or owned by another tenant.</summary>
    public static async Task<Result<Todo>> LoadOwnedAsync(
        this IEventSourcedRepository<Todo, Guid> repository,
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);

        Result<Todo> loaded = await repository
            .LoadAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return loaded.Ensure(
            todo => todo.TenantId == tenantId,
            RepositoryOutcome.Failure.NotFoundFor<Todo>(id));
    }
}
```

- [ ] **Step 3: Rewrite `CreateTodo`**

Handler dependencies become `IEventSourcedRepository<Todo, Guid>`, `ITenantContext`, `IUserContext`, `TimeProvider`. Body:

```csharp
Result<Guid> tenant = tenantContext.RequireTenant();
if (tenant.IsFailure)
    return Result<Response>.Fail(tenant.Errors);

DateTimeOffset nowUtc = clock.GetUtcNow();
string? actor = actorContext.Actor();

Result<Todo> created = Todo.Create(
    id: Guid.NewGuid(),
    tenantId: tenant.Value,
    title: request.Title,
    nowUtc: nowUtc,
    actor: actor,
    description: request.Description,
    priority: request.Priority ?? TodoConstant.Defaults.Priority,
    dueAtUtc: request.DueAtUtc,
    listId: request.ListId);
if (created.IsFailure)
    return Result<Response>.Fail(created.Errors);

foreach (Guid categoryId in request.CategoryIds)
{
    Result added = created.Value.AddCategory(categoryId, nowUtc, actor);
    if (added.IsFailure)
        return Result<Response>.Fail(added.Errors);
}

Result saved = await repository.SaveAsync(created.Value, cancellationToken).ConfigureAwait(false);

return saved.IsFailure
    ? Result<Response>.Fail(saved.Errors)
    : Result<Response>.Success(created.Value.ToModel<Response>());
```

- [ ] **Step 4: Rewrite `UpdateTodo`**

Load, compute the final state, raise one event:

```csharp
Result<Todo> loaded = await repository.LoadOwnedAsync(request.Id, tenant.Value, cancellationToken);
if (loaded.IsFailure)
    return Result<Response>.Fail(loaded.Errors);

Todo todo = loaded.Value;
string title = request.Title ?? todo.Title;
string? description = request.ClearDescription ? null : request.Description ?? todo.Description;
TodoPriority priority = request.Priority ?? todo.Priority;
DateTimeOffset? dueAtUtc = request.ClearDueAt ? null : request.DueAtUtc ?? todo.DueAtUtc;
Guid? listId = request.ClearList ? null : request.ListId ?? todo.ListId;

Result updated = todo.Update(title, description, priority, dueAtUtc, listId, clock.GetUtcNow(), actor);
if (updated.IsFailure)
    return Result<Response>.Fail(updated.Errors);

Result saved = await repository.SaveAsync(todo, cancellationToken).ConfigureAwait(false);
return saved.IsFailure
    ? Result<Response>.Fail(saved.Errors)
    : Result<Response>.Success(todo.ToModel<Response>());
```

- [ ] **Step 5: Rewrite the transition handlers**

Use the same shape in `StartTodo`, `CompleteTodo`, `CancelTodo`, `ReopenTodo`:

```csharp
Result<Todo> loaded = await repository.LoadOwnedAsync(request.Id, tenant.Value, cancellationToken);
if (loaded.IsFailure)
    return Result<Response>.Fail(loaded.Errors);

Result transition = loaded.Value.Start(clock.GetUtcNow(), actorContext.Actor());
if (transition.IsFailure)
    return Result<Response>.Fail(transition.Errors);

Result saved = await repository.SaveAsync(loaded.Value, cancellationToken).ConfigureAwait(false);
return saved.IsFailure
    ? Result<Response>.Fail(saved.Errors)
    : Result<Response>.Success(loaded.Value.ToModel<Response>());
```

Substitute `Complete`, `Cancel`, `Reopen` and pass `nowUtc` once.

- [ ] **Step 6: Rewrite delete/restore**

`DeleteTodo`:

```csharp
Result<Todo> loaded = await repository.LoadOwnedAsync(request.Id, tenant.Value, cancellationToken);
if (loaded.IsFailure)
    return Result.Fail(loaded.Errors);

Result deleted = loaded.Value.Delete(request.Mode == DeleteMode.Hard, clock.GetUtcNow(), actorContext.Actor());
if (deleted.IsFailure)
    return deleted;

return await repository.SaveAsync(loaded.Value, cancellationToken).ConfigureAwait(false);
```

`RestoreTodo` loads, calls `Restore(now, actor)`, saves and maps the response.

- [ ] **Step 7: Run the command tests**

```bash
dotnet build tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build --filter-class "*EventSourcedTodoCommandsTest*"
```

- [ ] **Step 8: Commit**

```bash
git add src/Application/Features/Todos tests/Unit/Application.Unit.Tests/Features/Todos
git commit -m "feat(app): write todo items through the event store"
```

---

### Task 4: Category/List Commands and Reference Cleanup

**Files:**
- Modify: `src/Application/Features/Todos/Items/Commands/AddTodoCategory.cs`
- Modify: `src/Application/Features/Todos/Items/Commands/RemoveTodoCategory.cs`
- Modify: `src/Application/Features/Todos/Items/Commands/AssignTodoToList.cs`
- Modify: `src/Application/Features/Todos/Items/Commands/ClearTodoList.cs`
- Modify: `src/Infrastructure/Services/Todos/TodoReferenceCleaner.cs`
- Test: `tests/Unit/Application.Unit.Tests/Features/Todos/Items/Commands/EventSourcedTodo.Categories.Test.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Persistence/Todos/TodoReferenceCleaner.Test.cs` (rewrite)

**Interfaces:**
- Consumes: Task 1 category/list methods, Task 3 `LoadOwnedAsync`, `IEventSourcedRepository<Todo, Guid>`.
- Produces: the same `ITodoReferenceCleaner` contract; it now appends `TodoListChanged` / `TodoCategoryRemoved` events per affected stream.

- [ ] **Step 1: Rewrite the four item handlers**

Each handler loads the owned aggregate, calls the matching domain method with `clock.GetUtcNow()` and `actorContext.Actor()`, saves, and maps the response. Example for `AddTodoCategory`:

```csharp
Result<Todo> loaded = await repository.LoadOwnedAsync(request.Id, tenant.Value, cancellationToken);
if (loaded.IsFailure)
    return Result<Response>.Fail(loaded.Errors);

Result added = loaded.Value.AddCategory(request.CategoryId, clock.GetUtcNow(), actorContext.Actor());
if (added.IsFailure)
    return Result<Response>.Fail(added.Errors);

Result saved = await repository.SaveAsync(loaded.Value, cancellationToken).ConfigureAwait(false);
return saved.IsFailure
    ? Result<Response>.Fail(saved.Errors)
    : Result<Response>.Success(loaded.Value.ToModel<Response>());
```

- [ ] **Step 2: Rewrite `TodoReferenceCleaner`**

Use the read model to find affected ids, then append events through the repository:

```csharp
public sealed class TodoReferenceCleaner(
    ApplicationDbContext context,
    IEventSourcedRepository<Todo, Guid> repository,
    TimeProvider clock) : ITodoReferenceCleaner
{
    public async Task<Result<int>> ClearListAsync(
        Guid tenantId, Guid listId, DateTimeOffset nowUtc, string? actor,
        CancellationToken cancellationToken = default)
    {
        List<Guid> ids = await context.Set<TodoReadModel>()
            .Where(todo => todo.TenantId == tenantId && todo.ListId == listId)
            .Select(todo => todo.Id)
            .ToListAsync(cancellationToken);

        return await CleanAsync(ids, tenantId, nowUtc, actor,
            (todo, at, by) => todo.ClearList(at, by, allowDeleted: true), cancellationToken);
    }

    public async Task<Result<int>> RemoveCategoryAsync(
        Guid tenantId, Guid categoryId, DateTimeOffset nowUtc, string? actor,
        CancellationToken cancellationToken = default)
    {
        List<TodoReadModel> rows = await context.Set<TodoReadModel>()
            .Where(todo => todo.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        List<Guid> ids = [.. rows
            .Where(todo => todo.CategoryIds.Contains(categoryId))
            .Select(todo => todo.Id)];

        return await CleanAsync(ids, tenantId, nowUtc, actor,
            (todo, at, by) => todo.RemoveCategory(categoryId, at, by, allowDeleted: true),
            cancellationToken);
    }

    private async Task<Result<int>> CleanAsync(
        IReadOnlyList<Guid> ids,
        Guid tenantId,
        DateTimeOffset nowUtc,
        string? actor,
        Func<Todo, DateTimeOffset, string?, Result> mutate,
        CancellationToken cancellationToken)
    {
        int changed = 0;
        foreach (Guid id in ids)
        {
            Result<Todo> loaded = await repository.LoadOwnedAsync(
                id, tenantId, cancellationToken, includeDeleted: true);
            if (loaded.IsFailure)
                return Result<int>.Fail(loaded.Errors);

            Result mutated = mutate(loaded.Value, nowUtc, actor);
            if (mutated.IsFailure)
                return Result<int>.Fail(mutated.Errors);

            Result saved = await repository.SaveAsync(loaded.Value, cancellationToken);
            if (saved.IsFailure)
                return Result<int>.Fail(saved.Errors);

            changed++;
        }

        return Result<int>.Success(changed);
    }
}
```

The cleaner no longer needs `IUnitOfWork`; delete that dependency.

The cleaner keeps targeting every projection row, including soft-deleted ones, so a restored todo never keeps a dangling list or category reference. It reaches those streams with `LoadOwnedAsync(..., includeDeleted: true)` and mutates through the aggregate's `allowDeleted` escape hatch; user commands never pass it and stay blocked. Wrap the query plus mutation loop in `try`/`catch (Exception exception) when (exception is not OperationCanceledException)` and return `Result<int>.Fail(UnitOfWorkOutcome.Failure.InvalidOperationFor(exception.Message))`; cancellation propagates.

- [ ] **Step 3: Update the delete-list/category command expectations**

`DeleteTodoList` and `DeleteTodoCategory` already call the cleaner before saving the list/category. Keep that order. Explain the cross-stream tradeoff in a `<remarks>` block on the handler class (not an inline `// Note:`):

```csharp
/// <summary>Deletes a list and clears the list reference from every todo.</summary>
/// <remarks>
/// Each todo stream commits independently, so a failed list delete can leave
/// references cleared. The operation is idempotent and safe to retry.
/// </remarks>
```

- [ ] **Step 4: Rewrite the tests**

`TodoReferenceCleaner.Test` builds an `ApplicationDbContext` over SQLite with `TodoReadModel` rows and a fake `IEventSourcedRepository<Todo, Guid>` that records saved aggregates. Assert the correct number of streams changed and that only matching tenants/lists/categories are touched. Add a soft-deleted row case: the cleaner clears its reference (the aggregate keeps `IsDeleted`) so a later restore has no dangling reference.

- [ ] **Step 5: Run**

```bash
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --filter-class "*EventSourcedTodoCategoriesTest*"
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --filter-class "*TodoReferenceCleanerTest*"
```

- [ ] **Step 6: Commit**

```bash
git add src/Application/Features/Todos src/Infrastructure/Services/Todos tests/Unit
git commit -m "feat(app): move todo references onto the event stream"
```

---

### Task 5: Read-Side Queries

**Files:**
- Modify: `src/Application/Features/Todos/Items/Queries/GetTodo.cs`
- Modify: `src/Application/Features/Todos/Items/Queries/SearchTodos.cs`
- Modify: `src/Application/Features/Todos/Items/Queries/SearchTodos.Tabular.cs`
- Modify: `src/Application/Features/Todos/Items/Models/Todo.Model.cs` (only if a query type needs it)
- Test: `tests/Unit/Application.Unit.Tests/Features/Todos/Items/Queries/EventSourcedTodo.Queries.Test.cs`

**Interfaces:**
- Consumes: `TodoReadModel`, `IPersistentRepository<TodoReadModel, Guid>`, `IMappableRepository<TodoReadModel, Guid>`, `IProjectionRepository<TodoReadModel, Guid>`, `TodoReadModelQueryMap`.
- Produces: unchanged `GetTodo.Response`, `SearchTodos.Response`, `SearchTodosTabular.Response`. No query loads an event stream.

- [ ] **Step 1: Write the failing query tests**

Use mocks of the read-model repositories and assert:
- `GetTodo` returns the row and hides another tenant's row and soft-deleted rows by default;
- `SearchTodos` builds a query with tenant + deletion scope and maps `TodoReadModel` to the summary;
- `SearchTodosTabular` queries `TodoReadModel` and returns the same columns.

- [ ] **Step 2: Change the query handlers**

`GetTodo`:

```csharp
public sealed class UseCaseHandler(
    IPersistentRepository<TodoReadModel, Guid> repository,
    ITenantContext tenantContext)
    : IQueryHandler<UseCase, Response>
```

The body keeps `tenantContext.RequireTenant().BindAsync(... repository.FindOwnedAsync(...))` and maps `todo.ToModel<Response>()`.

`SearchTodos` and `SearchTodos.Tabular` change their repository generic from `Todo` to `TodoReadModel` and map with the read-model overloads. Keep `ApplyTodoScope`.

- [ ] **Step 3: Update the mapper tests**

Move the old `Todo.Mapping.Unit.Test` query-map assertions to `TodoReadModel` and assert every filter/sort field still translates.

- [ ] **Step 4: Run**

```bash
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --filter-class "*EventSourcedTodoQueriesTest*"
```

- [ ] **Step 5: Commit**

```bash
git add src/Application/Features/Todos tests/Unit/Application.Unit.Tests/Features/Todos
git commit -m "feat(app): read todos from the projection"
```

---

### Task 6: Integration Event Bridge

**Files:**
- Create: `src/Application/Features/Todos/Events/TodoStatusChanged.Integration.Handler.cs`
- Delete: `src/Application/Features/Todos/Events/TodoCompleted.Integration.Handler.cs`
- Test: `tests/Unit/Application.Unit.Tests/Features/Todos/Events/TodoStatusChanged.IntegrationHandler.Test.cs`
- Delete: `tests/Unit/Application.Unit.Tests/Features/Todos/Events/TodoCompleted.IntegrationHandler.Test.cs`

**Interfaces:**
- Consumes: `TodoStatusChanged` (Task 1), `TodoCompletedIntegration`, `IIntegrationEventPublisher`.
- Produces: `todo.completed` is published exactly when `To == TodoStatus.Completed`; other transitions publish nothing.

- [ ] **Step 1: Write the failing bridge tests**

Cover completed → one `TodoCompletedIntegration` with `CompletedAtUtc == OccurredAtUtc`; started/cancelled/reopened → no publish; publisher failure → `ResultException`; metadata tenant is stamped.

- [ ] **Step 2: Replace the handler**

```csharp
using Application.Common.Messaging.Bus;

using Core.Common.Monad.Metadata;
using Core.Todos.Items;

using MediatR;

namespace Application.Features.Todos.Events;

/// <summary>Bridges completed status changes to the integration bus.</summary>
public sealed class TodoStatusChangedIntegrationHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<TodoStatusChanged>
{
    /// <inheritdoc />
    public async Task Handle(
        TodoStatusChanged notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // Ignore: only completion has an external contract today.
        if (notification.To != TodoStatus.Completed)
            return;

        var integration = new TodoCompletedIntegration
        {
            TodoId = notification.TodoId,
            TenantId = notification.TenantId,
            CompletedAtUtc = notification.OccurredAtUtc,
            Metadata = new DefaultMetadata(notification.Metadata),
        };

        integration.SetTenantId(notification.TenantId);

        Result result = await publisher
            .PublishAsync(integration, cancellationToken)
            .ConfigureAwait(false);

        result.ThrowIfFailure();
    }
}
```

`TodoStatusChanged` carries `TenantId` from Task 1, so the bridge never re-reads the stream.

- [ ] **Step 3: Delete the old bridge and its test**

The old `TodoCompleted` event no longer exists.

- [ ] **Step 4: Run**

```bash
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --filter-class "*TodoStatusChangedIntegrationHandlerTest*"
```

- [ ] **Step 5: Commit**

```bash
git add src/Application/Features/Todos/Events tests/Unit/Application.Unit.Tests/Features/Todos/Events
git commit -m "feat(app): bridge completed status changes to the bus"
```

---

### Task 7: Todo Backfill

**Files:**
- Modify: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/Options/EventStoreSetting.cs`
- Modify: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/Options/EventStoreSetting.Constant.cs`
- Create: `src/Infrastructure/Persistence/Schemas/Todos/Seeders/TodoBackfill.Seeder.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Persistence/Todos/Seeders/TodoBackfill.Seeder.Test.cs`
- Test: `tests/Integration/Infrastructure.Integration.Tests/EventSourcing/TodoBackfill.Integration.Test.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext`, `TodoReadModel`, `IEventStore`, `ISnapshotStore`, `IProjectionCheckpointStore`, `EventStoreSetting`.
- Produces: `TodoBackfillSeeder` (`Name = "seeder:todo-backfill"`, `Order = 35`) and setting `BackfillTodos` (default `false`).

> **Load-bearing:** a legacy `todos` row carries an aggregate `Version` written by the old
> repository, not a stream version. Before the backfill appends events for that row, reset the
> row's `Version` to `0` before the projection resumes. Otherwise the projection's
> `row.Version < incomingVersion` guard sees the stale large version and skips or misapplies the
> replayed events. Step 2 must assert this explicitly.

- [ ] **Step 1: Add settings**

`EventStoreSetting`:

```csharp
public bool BackfillTodos { get; set; } = EventStoreSettingConstant.Defaults.BackfillTodos;
```

`EventStoreSettingConstant`:

```csharp
public const bool BackfillTodos = false;
```

- [ ] **Step 2: Write the failing backfill test**

Unit test with SQLite `ApplicationDbContext` rows and mock event/snapshot/checkpoint stores. Assert:
- disabled setting → no appends;
- a row without a stream appends `TodoCreated` + status/category/list/delete events needed to match state, saves a snapshot, and writes a `TodoBackfill:{id}` marker;
- a row without a stream has its `Version` reset to `0` before projection resumes, so the replayed stream is not skipped;
- a row with an existing stream is skipped;
- re-running appends nothing.

- [ ] **Step 3: Implement the seeder**

```csharp
public sealed class TodoBackfillSeeder(
    ApplicationDbContext context,
    IEventStore eventStore,
    ISnapshotStore snapshotStore,
    IProjectionCheckpointStore checkpoints,
    IOptions<EventStoreSetting> setting,
    TimeProvider clock) : IPersistenceSeeder<TodoReadModel, Guid>
{
    public string Name => "seeder:todo-backfill";
    public string DisplayName => "Backfill Todo event streams";
    public int Order => 35;
    public string[] DependsOn => ["seeder:todos"];
    public bool IsRequired => false;

    public async Task<Result> ExecuteAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        if (!setting.Value.BackfillTodos)
            return Result.Success();

        List<TodoReadModel> rows = await context.Set<TodoReadModel>()
            .AsNoTracking()
            .OrderBy(todo => todo.CreatedAtUtc)
            .Take(setting.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (TodoReadModel row in rows)
        {
            string streamId = EventStreamName.For<Todo>(row.Id);
            Result<IReadOnlyList<StoredEvent>> existing =
                await eventStore.ReadStreamAsync(streamId, 1, cancellationToken);
            if (existing.IsSuccess)
                continue;

            IReadOnlyList<IDomainEvent> events = BuildEvents(row);

            // Reset: a legacy row's aggregate Version is not a stream version; zero it so the
            // projection's Version guard applies the replayed events.
            await context.Set<TodoReadModel>()
                .Where(todo => todo.Id == row.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(todo => todo.Version, 0L),
                    cancellationToken);

            Result appended = await eventStore.AppendAsync(
                streamId, 0, events, cancellationToken);
            if (appended.IsFailure)
                return appended;

            string state = JsonSerializer.Serialize(ToSnapshot(row, events.Count));
            Result snapshot = await snapshotStore.SaveAsync(
                new Snapshot(streamId, events.Count, state, clock.GetUtcNow()),
                cancellationToken);
            if (snapshot.IsFailure)
                return snapshot;

            Result marker = await checkpoints.SavePositionAsync(
                $"TodoBackfill:{row.Id}", events.Count, cancellationToken);
            if (marker.IsFailure)
                return marker;
        }

        return Result.Success();
    }
}
```

`BuildEvents`:

```csharp
private static IReadOnlyList<IDomainEvent> BuildEvents(TodoReadModel row)
{
    List<IDomainEvent> events =
    [
        new TodoCreated(
            row.Id, row.TenantId, row.Title, row.Description, row.Priority,
            row.DueAtUtc, row.ListId, row.CreatedAtUtc, row.CreatedBy),
    ];

    foreach (Guid categoryId in row.CategoryIds)
        events.Add(new TodoCategoryAdded(row.Id, categoryId, row.CreatedAtUtc, row.CreatedBy));

    if (row.ModifiedAtUtc is not null)
    {
        events.Add(new TodoUpdated(
            row.Id, row.Title, row.Description, row.Priority, row.DueAtUtc,
            row.ListId, row.ModifiedAtUtc.Value, row.ModifiedBy));
    }

    if (row.Status != TodoStatus.Pending)
    {
        events.Add(new TodoStatusChanged(
            row.Id, row.TenantId, TodoStatus.Pending, row.Status,
            row.CompletedAtUtc ?? row.ModifiedAtUtc ?? row.CreatedAtUtc,
            row.ModifiedBy));
    }

    if (row.IsDeleted)
    {
        events.Add(new TodoDeleted(
            row.Id, false,
            row.DeletedAtUtc ?? row.ModifiedAtUtc ?? row.CreatedAtUtc,
            row.DeletedBy));
    }

    return events;
}
```

`ToSnapshot` returns the same shape as `TodoSnapshot`, with `Version = events.Count`.

- [ ] **Step 4: Add the PostgreSQL integration test**

Use the messaging fixture to seed a `todos` row manually through `TodoReadModel` (or raw SQL) with a stale non-zero `Version`, run the seeder, and assert the row's `Version` is `0` immediately after the backfill. Then run the projection runner and assert the row's `Version` equals the stream event count and every field matches the seeded row. Run the seeder twice and assert no duplicate events.

- [ ] **Step 5: Run**

```bash
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --filter-class "*TodoBackfillSeederTest*"
dotnet test --project tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release --filter-class "*TodoBackfillIntegrationTest*"
```

- [ ] **Step 6: Commit**

```bash
git add src/Infrastructure/Persistence tests/Unit tests/Integration
git commit -m "feat(infra): backfill todo streams from the read model"
```

---

### Task 8: Migrate the Existing Test Suite and Fixtures

**Files:**
- Modify: `tests/Integration/Infrastructure.Integration.Tests/Persistence/Todos/TodoEfCore.Fixture.cs`
- Modify: `tests/Integration/Infrastructure.Integration.Tests/Persistence/Todos/Todo.Handler.Integration.Test.cs`
- Modify: `tests/Integration/Infrastructure.Integration.Tests/Persistence/Todos/Todo.QueryBuilder.Integration.Test.cs`
- Modify: `tests/Integration/Infrastructure.Integration.Tests/Persistence/Todos/Todo.Tabular.Integration.Test.cs`
- Delete: `tests/Integration/Infrastructure.Integration.Tests/Persistence/Todos/Todo.Mapping.Integration.Test.cs`
- Delete: `tests/Integration/Infrastructure.Integration.Tests/Persistence/Todos/Todo.Repository.Integration.Test.cs`
- Delete: `tests/Integration/Infrastructure.Integration.Tests/Persistence/Todos/Todo.UnitOfWork.Integration.Test.cs`
- Delete: `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Repositories/Todo.Mapping.Unit.Test.cs`
- Modify: `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/DbContext/ApplicationDbContext.Test.cs`
- Modify: `tests/Integration/Infrastructure.Integration.Tests/Messaging/CommandPipeline.Integration.Test.cs`
- Modify: `tests/Integration/Infrastructure.Integration.Tests/Messaging/Inbox.Store.Integration.Test.cs`
- Modify: `tests/Integration/Infrastructure.Integration.Tests/Messaging/Outbox.Atomicity.Integration.Test.cs`
- Modify: `tests/Integration/Infrastructure.Integration.Tests/Persistence/Concurrency.Test.cs`
- Modify: `tests/Integration/Infrastructure.Integration.Tests/Persistence/Initialization/TodoSeeder.Integration.Test.cs`
- Modify: `tests/Unit/Infrastructure.Unit.Tests/Persistence/Todos/Seeders/Todo.Seeder.Test.cs`
- Modify: `tests/Unit/Infrastructure.Unit.Tests/Persistence/Todos/Seeders/Todo.Seeders.Sqlite.Test.cs`
- Modify: every Application command test under `tests/Unit/Application.Unit.Tests/Features/Todos/Items` and `.../Fixtures/Todo.Handler.Fixture.cs`

**Interfaces:**
- Consumes: Tasks 1–7.
- Produces: a green suite with no reference to `Todo` as an EF entity.

- [ ] **Step 1: Rewrite the fixture**

`TodoEfCoreFixture` exposes:
- `IEventSourcedRepository<Todo, Guid> TodoEvents`
- `IPersistentRepository<TodoReadModel, Guid> TodoReadRepository`
- `IMappableRepository<TodoReadModel, Guid> TodoMapperRepository`
- `IProjectionRepository<TodoReadModel, Guid> TodoProjectionRepository`
- `IEventStore EventStore`, `ISnapshotStore Snapshots`, `ProjectionRunner Runner`
- `QueryMap<TodoReadModel> TodoQueryMap`
- a `SaveTodoAsync(Todo)` helper that calls `TodoEvents.SaveAsync` and then `Runner.RunOnceAsync()`.

Register `AddEventStore`-equivalent services in the fixture exactly as `Messaging.Fixture` does, plus `DbContext`, `IUnitOfWork`, `ITodoReferenceCleaner`, and the Todo query map.

- [ ] **Step 2: Rewrite the handler integration test**

Rebuild `Todo.Handler.Integration.Test` against the new handlers. Keep every user-visible assertion:
- create/get/update/delete/restore;
- start/complete/cancel/reopen;
- add/remove category, assign/clear list;
- list/category deletion clears references;
- hard delete removes the projection row;
- every response still matches `TodoResponseModel`.

Use `SaveTodoAsync` only when the test needs a pre-existing todo; otherwise call the real command handler.

- [ ] **Step 3: Convert repo/query tests**

- `Todo.QueryBuilder.Integration.Test` and `Todo.Tabular.Integration.Test` query `TodoReadModel` and seed rows by inserting `TodoReadModel` directly with `DbContext`.
- Delete `Todo.Repository.Integration.Test`, `Todo.UnitOfWork.Integration.Test` and `Todo.Mapping.Integration.Test`; their remaining coverage moves to `Todo.Handler.Integration.Test` and the new `Todo.Projection.Test`.
- Delete `Todo.Mapping.Unit.Test`; replace its query-map coverage with `TodoReadModel.Mapping.Unit.Test` that registers `TodoReadModelQueryMap` and asserts filter/sort/projection translation over SQLite.

- [ ] **Step 4: Convert messaging tests**

Replace `context.Set<Todo>()` inserts with either:
- `context.Set<TodoReadModel>()` for outbox/inbox tests that only need a row, or
- `TodoEvents.SaveAsync` + `Runner.RunOnceAsync()` for tests that assert domain events reached the outbox.

`CommandPipeline.Integration.Test` must assert that `CompleteTodo` produces one `TodoStatusChanged` outbox row and one `todo.completed` integration row after dispatch.

- [ ] **Step 5: Convert the DbContext discovery test**

`ApplicationDbContext.Test`:
- replace the `Todo` entity assertion with `TodoReadModel`;
- assert `context.Set<Todo>()` throws (Todo is no longer mapped) or remove the assertion entirely;
- update the expected table list: `todos` stays, `todo_reminders` stays;
- assert a single `InitialCreate` migration; the earlier chain (7–8 per-feature migrations) was squashed in `bf6ef5e`, so do not expect `AddTodoReadModel` or the old count.

- [ ] **Step 6: Delete obsolete unit command tests or rewrite them**

For every file under `tests/Unit/Application.Unit.Tests/Features/Todos/Items/Commands`, replace `IPersistentRepository<Todo, Guid>` mocks with `IEventSourcedRepository<Todo, Guid>` mocks:
- `RegisterTodo(todo)` sets up `LoadAsync`.
- Assertions on `SaveNewAsync` / `SaveUpdateAsync` become assertions on `SaveAsync`.
- Hard-delete assertion becomes `todo.IsDeleted` + one `TodoDeleted` with `IsHard == true`.

Rewrite `Todo.Handler.Fixture`:

```csharp
public Mock<IEventSourcedRepository<Todo, Guid>> TodoEvents { get; } = new();
public Mock<IPersistentRepository<TodoReadModel, Guid>> TodoReads { get; } = new();
public Mock<IMappableRepository<TodoReadModel, Guid>> TodoMapperRepository { get; } = new();

public void RegisterTodo(Todo todo) =>
    TodoEvents
        .Setup(store => store.LoadAsync(todo.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result<Todo>.Success(todo));
```

Set `SaveAsync` to `Result.Ok()` in the constructor.

- [ ] **Step 7: Run the full .NET suite**

```bash
make test
```

Fix every remaining compile error before touching behaviour. Expected: all unit and API tests pass.

- [ ] **Step 8: Commit**

```bash
git add tests
git commit -m "test: migrate the todo suite to the event-sourced model"
```

---

### Task 9: Gates, Docs and Release Note

**Files:**
- Modify: `docs/specs/2026-10-29-event-sourcing-design.md` (status → Implemented)
- Modify: `docs/plans/2026-10-30-event-sourcing.md` (tick Tasks 1–6 and wave 2)
- Create: `docs/releases/2026-10-30-todo-event-sourcing.md`

- [ ] **Step 1: Run the gates**

```bash
make lint
make build
make test
make coverage.unit
make api.test
make infra.test
make structure.check
make lib.format.check
```

Expected: every command exits `0`; coverage floors pass; `infra.test` is `0 skipped`.

- [ ] **Step 2: Run the full gate when Docker is available**

```bash
make ci.full
```

- [ ] **Step 3: Write the release note**

Document: deploy all instances together; take a backup before backfill; `EventStore:BackfillTodos` is one-shot per row and idempotent; rollback restores the state-based app while streams remain.

- [ ] **Step 4: Commit**

```bash
git add docs
git commit -m "docs: record the todo event-sourcing release"
```

---

## Self-Review

| Spec section | Task |
|---|---|
| §8 target aggregate | Task 1 |
| §8 events | Task 1 |
| §8 read model / projection | Task 2 |
| §8 write path | Tasks 3–4 |
| §8 backfill | Task 7 |
| §8 migration order / rollback | Tasks 2, 7, 9 |
| §3–§6 wave-1 contracts reused | Tasks 2, 3, 7 |
| §10 unit/integration/API tests | Tasks 1–8 |
| §11 acceptance criteria | Task 9 |

Known deliberate deviations, recorded here so executors do not "fix" them:

1. `TodoStatusChanged` carries `TenantId` in addition to the spec's From/To/OccurredAt columns, because the integration bridge needs the tenant without re-reading the stream.
2. `TodoCategoryAdded/Removed`, `TodoListChanged`, `TodoDeleted/Restored` carry `OccurredAtUtc` and `Actor` so the read model's audit columns stay accurate without a second event.
3. `TodoDeleted` carries `IsHard` so the public hard-delete API keeps removing the projection row; the stream itself is never deleted.
4. No upcaster is added for old Todo event payloads: no Todo stream exists before this wave (wave 1 only wrote reminder streams, and the event-store migration is new). The serializer still fails loudly on an unknown `(name, version)`.
5. `TodoReferenceCleaner` commits one stream per affected todo. Cross-stream atomicity with the list/category delete is not possible without a process manager; the cleanup is idempotent and safe to retry. The delete commands keep the cleaner-before-delete order and document the tradeoff.
6. `ITodo` no longer inherits the CRUD/lifecycle concern interfaces; `EventSourcedAggregate` owns identity and version, and no external consumer uses them.
7. `EventStore:RebuildProjectionsOnStartup` remains a wave-1 follow-up. The Todo backfill resets the legacy row `Version` to `0`, so it does not depend on startup rebuild wiring.
6. `ITodo` no longer inherits the CRUD/lifecycle concern interfaces (`IAggregateRoot`, `ICreatable`, `IModifiable`, `ISoftDeletable`, `ITenantable<Guid>`). Those contracts require public setters and an `Events`/`Metadata` surface; `EventSourcedAggregate<Guid>` owns identity and version, so `ITodo` is now a get-only state contract. No external consumer uses the removed base interfaces.

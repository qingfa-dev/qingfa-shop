using System.Collections.ObjectModel;
using System.Text.Json;

using Core.Common.Domain.Events;
using Core.Common.Domain.EventSourcing;
using Core.Common.Monad.Metadata;

namespace Core.Todos.Items;

/// <summary>An event-sourced unit of work with a lifecycle, a priority, and optional references.</summary>
public sealed class Todo : EventSourcedAggregate<Guid>, ITodo, ISnapshotable
{
    private static readonly JsonSerializerOptions SnapshotOptions = new();

    private readonly List<Guid> _categoryIds = [];
    private readonly ReadOnlyCollection<Guid> _categoryIdsView;

    internal Todo()
    {
        Title = string.Empty;
        Status = TodoConstant.Defaults.Status;
        Priority = TodoConstant.Defaults.Priority;
        _categoryIdsView = _categoryIds.AsReadOnly();
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
    public ICollection<Guid> CategoryIds => _categoryIdsView;

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

    /// <summary>Gets a value indicating whether the todo was permanently deleted.</summary>
    public bool IsHardDeleted { get; private set; }

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
        DefaultMetadata? eventMetadata = metadata is null
            ? null
            : new DefaultMetadata(metadata.ToDictionary(pair => pair.Key, pair => pair.Value));

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
            CreatedBy = NormalizeActor(actor),
            Status = TodoConstant.Defaults.Status,
        };

        return Result<Todo>.Success(candidate)
            .EnsureCreatedAt()
            .Ensure(todo => TodoValidator.Validate(todo))
            .Bind(todo => todo.RaiseEvent(new TodoCreated(
                todo.Id,
                todo.TenantId,
                todo.Title,
                todo.Description,
                todo.Priority,
                todo.DueAtUtc,
                todo.ListId,
                todo.CreatedAtUtc,
                todo.CreatedBy,
                eventMetadata)));
    }

    /// <summary>Replaces the editable fields in one event.</summary>
    public Result<Todo> Update(
        string? title,
        string? description,
        TodoPriority priority,
        DateTimeOffset? dueAtUtc,
        Guid? listId,
        DateTimeOffset nowUtc,
        string? actor)
    {
        var normalizedTitle = title?.Trim() ?? string.Empty;
        var normalizedDescription = description?.Trim();
        var normalizedDue = dueAtUtc?.ToUniversalTime();

        return Result<Todo>.Success(this)
            .EnsureMutable()
            .EnsureTitle(normalizedTitle)
            .EnsureDescription(normalizedDescription)
            .EnsurePriority(priority)
            .EnsureDueDate(normalizedDue)
            .EnsureListId(listId)
            .Bind(todo => todo.RaiseEvent(new TodoUpdated(
                todo.Id,
                normalizedTitle,
                normalizedDescription,
                priority,
                normalizedDue,
                listId,
                nowUtc.ToUniversalTime(),
                NormalizeActor(actor))));
    }

    /// <summary>Moves a pending todo into progress.</summary>
    public Result<Todo> Start(DateTimeOffset nowUtc, string? actor)
        => Transition(TodoStatus.InProgress, nowUtc, actor);

    /// <summary>Completes a started todo.</summary>
    public Result<Todo> Complete(DateTimeOffset nowUtc, string? actor)
        => Transition(TodoStatus.Completed, nowUtc, actor);

    /// <summary>Cancels a pending or started todo.</summary>
    public Result<Todo> Cancel(DateTimeOffset nowUtc, string? actor)
        => Transition(TodoStatus.Cancelled, nowUtc, actor);

    /// <summary>Reopens a finished todo.</summary>
    public Result<Todo> Reopen(DateTimeOffset nowUtc, string? actor)
        => Transition(TodoStatus.Pending, nowUtc, actor);

    /// <summary>Assigns the todo to a list.</summary>
    public Result<Todo> AssignToList(Guid listId, DateTimeOffset nowUtc, string? actor)
        => Result<Todo>.Success(this)
            .EnsureMutable()
            .EnsureListId(listId)
            .Bind(todo => todo.RaiseEvent(new TodoListChanged(
                todo.Id,
                listId,
                nowUtc.ToUniversalTime(),
                NormalizeActor(actor))));

    /// <summary>Clears the list reference.</summary>
    /// <param name="nowUtc">The UTC moment of the change.</param>
    /// <param name="actor">The optional actor.</param>
    /// <param name="allowDeleted">
    /// True to mutate a soft-deleted stream during cross-aggregate reference cleanup.
    /// </param>
    public Result<Todo> ClearList(DateTimeOffset nowUtc, string? actor, bool allowDeleted = false)
        => Result<Todo>.Success(this)
            .EnsureMutable(allowDeleted)
            .Bind(todo => todo.RaiseEvent(new TodoListChanged(
                todo.Id,
                null,
                nowUtc.ToUniversalTime(),
                NormalizeActor(actor))));

    /// <summary>Adds a category reference.</summary>
    public Result<Todo> AddCategory(Guid categoryId, DateTimeOffset nowUtc, string? actor)
        => Result<Todo>.Success(this)
            .EnsureMutable()
            .EnsureCategories([.. _categoryIds, categoryId])
            .Bind(todo => todo.RaiseEvent(new TodoCategoryAdded(
                todo.Id,
                categoryId,
                nowUtc.ToUniversalTime(),
                NormalizeActor(actor))));

    /// <summary>Removes a category reference.</summary>
    /// <param name="categoryId">The category identifier to remove.</param>
    /// <param name="nowUtc">The UTC moment of the change.</param>
    /// <param name="actor">The optional actor.</param>
    /// <param name="allowDeleted">
    /// True to mutate a soft-deleted stream during cross-aggregate reference cleanup.
    /// </param>
    public Result<Todo> RemoveCategory(
        Guid categoryId,
        DateTimeOffset nowUtc,
        string? actor,
        bool allowDeleted = false)
        => Result<Todo>.Success(this)
            .EnsureMutable(allowDeleted)
            .Ensure(todo => todo.CategoryIds.Contains(categoryId), TodoOutcome.Failure.CategoryNotFound)
            .Bind(todo => todo.RaiseEvent(new TodoCategoryRemoved(
                todo.Id,
                categoryId,
                nowUtc.ToUniversalTime(),
                NormalizeActor(actor))));

    /// <summary>Soft- or hard-deletes the todo. A hard delete removes the projection row.</summary>
    public Result<Todo> Delete(bool isHard, DateTimeOffset nowUtc, string? actor)
    {
        // Handle: Repeating a soft delete is an intentional idempotent no-op; no event is raised.
        if (IsDeleted && !isHard)
            return Result<Todo>.Success(this);

        return Result<Todo>.Success(this)
            .Bind(todo => todo.RaiseEvent(new TodoDeleted(
                todo.Id,
                isHard,
                nowUtc.ToUniversalTime(),
                NormalizeActor(actor))));
    }

    /// <summary>Restores a soft-deleted todo.</summary>
    public Result<Todo> Restore(DateTimeOffset nowUtc, string? actor)
    {
        // Handle: Restoring a live todo is an intentional idempotent no-op; no event is raised.
        if (!IsDeleted)
            return Result<Todo>.Success(this);

        return Result<Todo>.Success(this)
            .Ensure(todo => !todo.IsHardDeleted, TodoOutcome.Failure.HardDeletedNotFound)
            .Bind(todo => todo.RaiseEvent(new TodoRestored(
                todo.Id,
                nowUtc.ToUniversalTime(),
                NormalizeActor(actor))));
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
            IsHardDeleted,
            DeletedAtUtc,
            DeletedBy,
            Version),
            SnapshotOptions);

    /// <inheritdoc />
    public void RestoreSnapshot(string state)
    {
        TodoSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<TodoSnapshot>(state, SnapshotOptions);
        }
        catch (JsonException exception)
        {
            // Boundary: a corrupt snapshot is a storage invariant failure; the repository falls back to replay.
            throw new InvalidOperationException("The todo snapshot is invalid.", exception);
        }

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
        IsHardDeleted = snapshot.IsHardDeleted;
        DeletedAtUtc = snapshot.DeletedAtUtc;
        DeletedBy = snapshot.DeletedBy;
        RestoreVersion(snapshot.Version);
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
                IsHardDeleted = false;
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
                IsHardDeleted = deleted.IsHard;
                DeletedAtUtc = deleted.OccurredAtUtc;
                DeletedBy = deleted.Actor;
                break;

            case TodoRestored restored:
                IsDeleted = false;
                IsHardDeleted = false;
                DeletedAtUtc = null;
                DeletedBy = null;
                ModifiedAtUtc = restored.OccurredAtUtc;
                ModifiedBy = restored.Actor;
                break;

            default:
                // Boundary: an event outside this aggregate's vocabulary means the stream is corrupt.
                throw new InvalidOperationException(
                    $"Event '{domainEvent.GetType().Name}' is not supported by Todo.");
        }
    }

    private static string? NormalizeActor(string? actor)
    {
        var trimmed = actor?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>Applies a validated event through the single mutation path.</summary>
    internal Result<Todo> RaiseEvent(IDomainEvent domainEvent)
        => TodoEventValidator.Validate(domainEvent)
            .Bind(() =>
            {
                Raise(domainEvent);
                return Result<Todo>.Success(this);
            });

    private Result<Todo> Transition(TodoStatus next, DateTimeOffset nowUtc, string? actor)
        => Result<Todo>.Success(this)
            .EnsureMutable()
            .EnsureTransition(next)
            .Bind(todo => todo.RaiseEvent(new TodoStatusChanged(
                todo.Id,
                todo.TenantId,
                todo.Status,
                next,
                nowUtc.ToUniversalTime(),
                NormalizeActor(actor))));
}

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

        // Guard: A created todo must carry its creation instant.
        if (candidate.CreatedAtUtc == default)
            return Result<Todo>.Fail(TodoOutcome.Failure.CreatedAtRequired);

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
        Result validation = TodoValidator.ValidateNotDeleted(IsDeleted);
        if (validation.IsFailure)
            return validation;

        var normalizedTitle = title?.Trim() ?? string.Empty;
        var normalizedDescription = description?.Trim();
        var normalizedDue = dueAtUtc?.ToUniversalTime();

        validation = TodoValidator.ValidateTitle(normalizedTitle);
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
            NormalizeActor(actor)));

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

        Raise(new TodoListChanged(Id, listId, nowUtc.ToUniversalTime(), NormalizeActor(actor)));
        return Result.Ok();
    }

    /// <summary>Clears the list reference.</summary>
    /// <param name="nowUtc">The UTC moment of the change.</param>
    /// <param name="actor">The optional actor.</param>
    /// <param name="allowDeleted">
    /// True to mutate a soft-deleted stream during cross-aggregate reference cleanup.
    /// </param>
    public Result ClearList(DateTimeOffset nowUtc, string? actor, bool allowDeleted = false)
    {
        Result validation = ValidateMutable(allowDeleted);
        if (validation.IsFailure)
            return validation;

        Raise(new TodoListChanged(Id, null, nowUtc.ToUniversalTime(), NormalizeActor(actor)));
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

        Raise(new TodoCategoryAdded(Id, categoryId, nowUtc.ToUniversalTime(), NormalizeActor(actor)));
        return Result.Ok();
    }

    /// <summary>Removes a category reference.</summary>
    /// <param name="categoryId">The category identifier to remove.</param>
    /// <param name="nowUtc">The UTC moment of the change.</param>
    /// <param name="actor">The optional actor.</param>
    /// <param name="allowDeleted">
    /// True to mutate a soft-deleted stream during cross-aggregate reference cleanup.
    /// </param>
    public Result RemoveCategory(
        Guid categoryId,
        DateTimeOffset nowUtc,
        string? actor,
        bool allowDeleted = false)
    {
        Result validation = ValidateMutable(allowDeleted);
        if (validation.IsFailure)
            return validation;

        if (!_categoryIds.Contains(categoryId))
            return Result.Fail(TodoOutcome.Failure.CategoryNotFound);

        Raise(new TodoCategoryRemoved(Id, categoryId, nowUtc.ToUniversalTime(), NormalizeActor(actor)));
        return Result.Ok();
    }

    /// <summary>Soft- or hard-deletes the todo. A hard delete removes the projection row.</summary>
    public Result Delete(bool isHard, DateTimeOffset nowUtc, string? actor)
    {
        // Handle: Repeating a soft delete is an intentional idempotent no-op; no event is raised.
        if (IsDeleted && !isHard)
            return Result.Ok();

        Raise(new TodoDeleted(Id, isHard, nowUtc.ToUniversalTime(), NormalizeActor(actor)));
        return Result.Ok();
    }

    /// <summary>Restores a soft-deleted todo.</summary>
    public Result Restore(DateTimeOffset nowUtc, string? actor)
    {
        // Guard: a hard-deleted stream has no projection row to restore.
        if (IsHardDeleted)
            return Result.Fail(TodoOutcome.Failure.HardDeletedNotFound);

        // Handle: Restoring a live todo is an intentional idempotent no-op; no event is raised.
        if (!IsDeleted)
            return Result.Ok();

        Raise(new TodoRestored(Id, nowUtc.ToUniversalTime(), NormalizeActor(actor)));
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

    /// <summary>Guards cleanup-aware mutators that may target a soft-deleted stream.</summary>
    private Result ValidateMutable(bool allowDeleted)
    {
        // Guard: a hard-deleted stream is permanently gone; even cleanup cannot append to it.
        if (IsHardDeleted)
            return Result.Fail(TodoOutcome.Failure.HardDeletedNotFound);

        return allowDeleted
            ? Result.Ok()
            : TodoValidator.ValidateNotDeleted(IsDeleted);
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
            NormalizeActor(actor)));

        return Result.Ok();
    }
}
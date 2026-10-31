using System.Diagnostics.CodeAnalysis;

using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Messaging.Serialization;
using Application.Modules.Todos.Items.Models;

using Core.BuildingBlocks.Domain.Events;
using Core.Modules.Todos.Items;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Todos.Persistence.Projections;

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
        model.ModifiedAtUtc = null;
        model.ModifiedBy = null;
        model.IsDeleted = false;
        model.DeletedAtUtc = null;
        model.DeletedBy = null;
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
using Application.Common.EventSourcing;
using Application.Features.Todos.Abstractions;
using Application.Features.Todos.Common;
using Application.Features.Todos.Items.Models;

using Core.Todos.Items;

using Infrastructure.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Todos;

/// <summary>Event-sourced implementation of <see cref="ITodoReferenceCleaner"/>.</summary>
/// <remarks>
/// Finds affected streams through the todo projection, then appends one
/// <c>TodoListChanged</c> or <c>TodoCategoryRemoved</c> event per stream.
/// </remarks>
/// <param name="context">The application DbContext.</param>
/// <param name="repository">The todo event-stream repository.</param>
public sealed class TodoReferenceCleaner(
    ApplicationDbContext context,
    IEventSourcedRepository<Todo, Guid> repository) : ITodoReferenceCleaner
{
    /// <inheritdoc />
    public async Task<Result<int>> ClearListAsync(
        Guid tenantId,
        Guid listId,
        DateTimeOffset nowUtc,
        string? actor,
        CancellationToken cancellationToken = default)
    {
        List<Guid> ids = await context.Set<TodoReadModel>()
            .Where(todo => todo.TenantId == tenantId && todo.ListId == listId)
            .Select(todo => todo.Id)
            .ToListAsync(cancellationToken);

        return await CleanAsync(
            ids,
            tenantId,
            nowUtc,
            actor,
            (todo, at, by) => todo.ClearList(at, by),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<int>> RemoveCategoryAsync(
        Guid tenantId,
        Guid categoryId,
        DateTimeOffset nowUtc,
        string? actor,
        CancellationToken cancellationToken = default)
    {
        // Load: category identifiers are a value-converted JSON array, so the provider
        // cannot translate a containment predicate. Scope by tenant and filter in memory.
        List<TodoReadModel> rows = await context.Set<TodoReadModel>()
            .Where(todo => todo.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        List<Guid> ids = [.. rows
            .Where(todo => todo.CategoryIds.Contains(categoryId))
            .Select(todo => todo.Id)];

        return await CleanAsync(
            ids,
            tenantId,
            nowUtc,
            actor,
            (todo, at, by) => todo.RemoveCategory(categoryId, at, by),
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
        var changed = 0;
        foreach (Guid id in ids)
        {
            Result<Todo> loaded = await repository
                .LoadOwnedAsync(id, tenantId, cancellationToken)
                .ConfigureAwait(false);
            if (loaded.IsFailure)
                return Result<int>.Fail(loaded.Errors);

            Result mutated = mutate(loaded.Value, nowUtc, actor);
            if (mutated.IsFailure)
                return Result<int>.Fail(mutated.Errors);

            Result saved = await repository
                .SaveAsync(loaded.Value, cancellationToken)
                .ConfigureAwait(false);
            if (saved.IsFailure)
                return Result<int>.Fail(saved.Errors);

            changed++;
        }

        return Result<int>.Success(changed);
    }
}

using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Abstractions;

using Core.Todos.Items;

using Infrastructure.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Todos;

/// <summary>
/// EF Core implementation of <see cref="ITodoReferenceCleaner"/>.
/// </summary>
/// <remarks>
/// Mutates tracked todos so the cleanup commits in the same
/// <c>SaveChanges</c> as the list or category delete.
/// </remarks>
/// <param name="context">The application DbContext.</param>
public sealed class TodoReferenceCleaner(ApplicationDbContext context) : ITodoReferenceCleaner
{
    /// <inheritdoc />
    public async Task<Result<int>> ClearListAsync(
        Guid tenantId,
        Guid listId,
        DateTimeOffset nowUtc,
        string? actor,
        CancellationToken cancellationToken = default)
    {
        try
        {
            List<Todo> todos = await context
                .Set<Todo>()
                .Where(todo => todo.TenantId == tenantId && todo.ListId == listId)
                .ToListAsync(cancellationToken);

            foreach (Todo todo in todos)
            {
                todo.ListId = null;
                Touch(todo, nowUtc, actor);
            }

            return Result<int>.Success(todos.Count);
        }
        catch (Exception exception)
        {
            return Result<int>.Fail(
                RepositoryOutcome.Failure.InvalidOperationFor(exception.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> RemoveCategoryAsync(
        Guid tenantId,
        Guid categoryId,
        DateTimeOffset nowUtc,
        string? actor,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Load: the category identifiers are a value-converted JSON array, so the
            // provider cannot translate a containment predicate. Scope by tenant only.
            List<Todo> todos = await context
                .Set<Todo>()
                .Where(todo => todo.TenantId == tenantId)
                .ToListAsync(cancellationToken);

            var changed = 0;
            foreach (Todo todo in todos)
            {
                if (!todo.CategoryIds.Remove(categoryId))
                {
                    continue;
                }

                Touch(todo, nowUtc, actor);
                changed++;
            }

            return Result<int>.Success(changed);
        }
        catch (Exception exception)
        {
            return Result<int>.Fail(
                RepositoryOutcome.Failure.InvalidOperationFor(exception.Message));
        }
    }

    private static void Touch(Todo todo, DateTimeOffset nowUtc, string? actor)
    {
        todo.ModifiedAtUtc = nowUtc.ToUniversalTime();
        todo.ModifiedBy = actor?.Trim();
    }
}

namespace Application.Modules.Todos.Abstractions;

/// <summary>
/// Removes cross-aggregate references when a list or category is deleted.
/// </summary>
/// <remarks>
/// Todos reference lists and categories by identifier, and category identifiers
/// live in a JSON array. The generic repository cannot express either cleanup, so
/// this narrow port stages the changes on the same unit of work as the delete.
/// </remarks>
public interface ITodoReferenceCleaner
{
    /// <summary>Clears <c>ListId</c> on every todo that references the list.</summary>
    /// <param name="tenantId">The tenant that owns the rows.</param>
    /// <param name="listId">The list being deleted.</param>
    /// <param name="nowUtc">The UTC moment of the change.</param>
    /// <param name="actor">The optional actor.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of todos updated, or a failure.</returns>
    Task<Result<int>> ClearListAsync(
        Guid tenantId,
        Guid listId,
        DateTimeOffset nowUtc,
        string? actor,
        CancellationToken cancellationToken = default);

    /// <summary>Removes the category identifier from every todo that references it.</summary>
    /// <param name="tenantId">The tenant that owns the rows.</param>
    /// <param name="categoryId">The category being deleted.</param>
    /// <param name="nowUtc">The UTC moment of the change.</param>
    /// <param name="actor">The optional actor.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of todos updated, or a failure.</returns>
    Task<Result<int>> RemoveCategoryAsync(
        Guid tenantId,
        Guid categoryId,
        DateTimeOffset nowUtc,
        string? actor,
        CancellationToken cancellationToken = default);
}

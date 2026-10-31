namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Categorizable;

/// <summary>Validation logic for <see cref="ICategorizable{TCategory}"/>.</summary>
public static class CategorizableValidator
{
    #region Public Methods

    /// <summary>
    /// Validates the category that
    /// <see cref="CategorizableExtensions.AddCategory{TValue, TCategory}"/>
    /// is about to add, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type.</typeparam>
    /// <typeparam name="TCategory">The category type.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="category">The category to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateAddCategory<TValue, TCategory>(
        TValue auditable,
        TCategory category)
        where TValue : ICategorizable<TCategory>
    {
        // Guard: Reject null entity before any validation.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                CategorizableOutcome.Failure.EntityRequired);
        }

        // Guard: Reject null category before any validation.
        if (category is null)
        {
            return Result<TValue>.Fail(
                CategorizableOutcome.Failure.CategoryRequired);
        }

        // Guard: Reject empty Guid identifiers before any validation.
        if (category is Guid guid && guid == Guid.Empty)
        {
            return Result<TValue>.Fail(
                CategorizableOutcome.Failure.CategoryEmpty);
        }

        // Compute: Normalize string categories for length and duplicate checks.
        var normalized = Normalize(category);

        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ => normalized is not string name
                    || !string.IsNullOrWhiteSpace(name),
                error: CategorizableOutcome.Failure.CategoryRequired)

            .Ensure(
                predicate: _ => normalized is not string name
                    || name.Length
                        <= CategorizableConstant.Constraints.Category.MaxNameLength,
                error: CategorizableOutcome.Failure.CategoryTooLong)

            // Compute: Ensure the category is not already assigned.
            .Ensure(
                predicate: entity => !entity.Categories.Contains(normalized),
                error: CategorizableOutcome.Failure.CategoryDuplicate)

            // Fallback: Ensure the entity has room for another category.
            .Ensure(
                predicate: entity =>
                    entity.Categories.Count
                        < CategorizableConstant.Constraints.Category.MaxCount,
                error: CategorizableOutcome.Failure.CategoryLimitExceeded);
    }

    /// <summary>
    /// Validates that the category that
    /// <see cref="CategorizableExtensions.RemoveCategory{TValue, TCategory}"/>
    /// targets is assigned, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type.</typeparam>
    /// <typeparam name="TCategory">The category type.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="category">The category to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateRemoveCategory<
        TValue,
        TCategory>(
        TValue auditable,
        TCategory category)
        where TValue : ICategorizable<TCategory>
    {
        // Guard: Reject null entity before any validation.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                CategorizableOutcome.Failure.EntityRequired);
        }

        // Guard: Reject null category before any validation.
        if (category is null)
        {
            return Result<TValue>.Fail(
                CategorizableOutcome.Failure.CategoryRequired);
        }

        // Compute: Normalize string categories so padded input still matches.
        var normalized = Normalize(category);

        // Compute: Ensure the category is currently assigned.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: entity => entity.Categories.Contains(normalized),
                error: CategorizableOutcome.Failure.CategoryNotFound);
    }

    #endregion

    #region Internal Methods

    /// <summary>Normalizes a category value (trims strings when enabled).</summary>
    internal static TCategory Normalize<TCategory>(TCategory category)
    {
        if (CategorizableConstant.Defaults.TrimValues
            && category is string name)
        {
            return (TCategory)(object)name.Trim();
        }

        return category;
    }

    #endregion
}
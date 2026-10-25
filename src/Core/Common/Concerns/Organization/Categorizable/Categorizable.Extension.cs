namespace Core.Common.Concerns.Organization.Categorizable;

/// <summary>Extension methods for <see cref="ICategorizable{TCategory}"/>.</summary>
public static class CategorizableExtensions
{
    #region Public Methods

    /// <summary>
    /// Adds a category to the entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// String categories are trimmed before validation.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="ICategorizable{TCategory}"/>.</typeparam>
    /// <typeparam name="TCategory">The category type.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="category">The category to add.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> AddCategory<TValue, TCategory>(
        this Result<TValue> result,
        TCategory category)
        where TValue : ICategorizable<TCategory>
    {
        // Compute: Normalize input so padded strings match stored values.
        var normalized = CategorizableValidator.Normalize(category);

        return result
            .Bind(entity =>
                CategorizableValidator.ValidateAddCategory(entity, normalized))
            .Tap(entity =>
            {
                entity.Categories.Add(normalized);
            });
    }

    /// <summary>
    /// Adds multiple categories to the entity atomically.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="ICategorizable{TCategory}"/>.</typeparam>
    /// <typeparam name="TCategory">The category type.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="categories">The categories to append.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> AddCategories<TValue, TCategory>(
        this Result<TValue> result,
        IEnumerable<TCategory> categories)
        where TValue : ICategorizable<TCategory>
    {
        // Compute: Normalize the batch once so validation and mutation agree.
        var normalized = categories
            .Select(CategorizableValidator.Normalize)
            .ToList();

        return result
            .Bind(entity =>
            {
                // Guard: Validate the whole batch before mutating anything.
                var validation = Result<TValue>.Success(entity);

                foreach (var category in normalized)
                {
                    validation = validation.Bind(current =>
                        CategorizableValidator.ValidateAddCategory(
                            current,
                            category));

                    if (validation.IsFailure)
                    {
                        return validation;
                    }
                }

                // Fallback: Ensure the batch fits within the max count.
                if (entity.Categories.Count + normalized.Count
                    > CategorizableConstant.Constraints.Category.MaxCount)
                {
                    return Result<TValue>.Fail(
                        CategorizableResult.Failure.CategoryLimitExceeded);
                }

                return validation;
            })
            .Tap(entity =>
            {
                foreach (var category in normalized)
                {
                    entity.Categories.Add(category);
                }
            });
    }

    /// <summary>
    /// Removes a category from the entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="ICategorizable{TCategory}"/>.</typeparam>
    /// <typeparam name="TCategory">The category type.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="category">The category to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> RemoveCategory<TValue, TCategory>(
        this Result<TValue> result,
        TCategory category)
        where TValue : ICategorizable<TCategory>
    {
        // Compute: Normalize input so padded strings match stored values.
        var normalized = CategorizableValidator.Normalize(category);

        return result
            .Bind(entity =>
                CategorizableValidator.ValidateRemoveCategory(entity, normalized))
            .Tap(entity =>
            {
                entity.Categories.Remove(normalized);
            });
    }

    /// <summary>
    /// Replaces all categories with the given set.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="ICategorizable{TCategory}"/>.</typeparam>
    /// <typeparam name="TCategory">The category type.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="categories">The replacement categories.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> SetCategories<TValue, TCategory>(
        this Result<TValue> result,
        IEnumerable<TCategory> categories)
        where TValue : ICategorizable<TCategory>
    {
        // Compute: Normalize and de-duplicate the incoming set.
        var normalized = categories
            .Select(CategorizableValidator.Normalize)
            .Distinct()
            .ToList();

        return result
            .Bind(entity =>
            {
                // Guard: Reject null entity before any validation.
                if (entity is null)
                {
                    return Result<TValue>.Fail(
                        CategorizableResult.Failure.EntityRequired);
                }

                // Fallback: Ensure the replacement fits within the max count.
                if (normalized.Count
                    > CategorizableConstant.Constraints.Category.MaxCount)
                {
                    return Result<TValue>.Fail(
                        CategorizableResult.Failure.CategoryLimitExceeded);
                }

                return Result<TValue>.Success(entity);
            })
            .Tap(entity =>
            {
                entity.Categories.Clear();

                foreach (var category in normalized)
                {
                    entity.Categories.Add(category);
                }
            });
    }

    /// <summary>
    /// Removes all categories from the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="ICategorizable{TCategory}"/>.</typeparam>
    /// <typeparam name="TCategory">The category type.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ClearCategories<TValue, TCategory>(
        this Result<TValue> result)
        where TValue : ICategorizable<TCategory>
    {
        return result.Tap(entity =>
        {
            entity.Categories.Clear();
        });
    }

    #endregion
}
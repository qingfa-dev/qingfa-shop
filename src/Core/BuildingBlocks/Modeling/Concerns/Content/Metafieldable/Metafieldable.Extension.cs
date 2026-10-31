namespace Core.BuildingBlocks.Modeling.Concerns.Content.Metafieldable;

/// <summary>Extension methods for the <see cref="IMetafieldable"/> concern.</summary>
public static class MetafieldableExtensions
{
    #region Public Methods

    /// <summary>
    /// Sets the metafield for a (namespace, key) pair, adding it when new.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the metafieldable entity.</typeparam>
    /// <param name="result">The result containing the metafieldable entity.</param>
    /// <param name="namespace">The metafield namespace.</param>
    /// <param name="key">The metafield key.</param>
    /// <param name="type">The metafield type.</param>
    /// <param name="value">The metafield value.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> SetMetafield<TValue>(
        this Result<TValue> result,
        string? @namespace,
        string? key,
        MetafieldType type,
        string? value)
        where TValue : IMetafieldable
    {
        return result
            .Bind(entity =>
                MetafieldableValidator.ValidateMetafield(
                    entity,
                    @namespace,
                    key,
                    value))

            .Tap(entity =>
            {
                var existing = entity.Metafields.FirstOrDefault(metafield =>
                    metafield.Namespace == @namespace &&
                    metafield.Key == key);

                if (existing is null)
                {
                    entity.Metafields.Add(new Metafield
                    {
                        Namespace = @namespace!,
                        Key = key!,
                        Type = type,
                        Value = value
                    });
                }
                else
                {
                    existing.Type = type;
                    existing.Value = value;
                }
            });
    }

    /// <summary>
    /// Sets the metafield for a (namespace, key) pair using the default
    /// <see cref="MetafieldableConstant.Defaults.Type">metafield type</see>,
    /// adding it when new.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the metafieldable entity.</typeparam>
    /// <param name="result">The result containing the metafieldable entity.</param>
    /// <param name="namespace">The metafield namespace.</param>
    /// <param name="key">The metafield key.</param>
    /// <param name="value">The metafield value.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> SetMetafield<TValue>(
        this Result<TValue> result,
        string? @namespace,
        string? key,
        string? value)
        where TValue : IMetafieldable
    {
        return result.SetMetafield(
            @namespace,
            key,
            MetafieldableConstant.Defaults.Type.Default,
            value);
    }

    /// <summary>
    /// Removes the metafield for a (namespace, key) pair.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the metafieldable entity.</typeparam>
    /// <param name="result">The result containing the metafieldable entity.</param>
    /// <param name="namespace">The metafield namespace.</param>
    /// <param name="key">The metafield key.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> RemoveMetafield<TValue>(
        this Result<TValue> result,
        string? @namespace,
        string? key)
        where TValue : IMetafieldable
    {
        return result
            .Bind(entity =>
                MetafieldableValidator.ValidateMetafieldRemoval(
                    entity,
                    @namespace,
                    key))

            .Tap(entity =>
            {
                var existing = entity.Metafields.First(metafield =>
                    metafield.Namespace == @namespace &&
                    metafield.Key == key);

                entity.Metafields.Remove(existing);
            });
    }

    /// <summary>
    /// Removes all metafields from the entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the metafieldable entity.</typeparam>
    /// <param name="result">The result containing the metafieldable entity.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> ClearMetafields<TValue>(
        this Result<TValue> result)
        where TValue : IMetafieldable
    {
        return result
            .Bind(entity =>
                MetafieldableValidator.ValidateClearMetafields(entity))
            .Tap(entity =>
            {
                entity.Metafields.Clear();
            });
    }

    /// <summary>Gets the metafield for a (namespace, key) pair, if present.</summary>
    /// <typeparam name="TValue">The type of the metafieldable entity.</typeparam>
    /// <param name="entity">The metafieldable entity.</param>
    /// <param name="namespace">The metafield namespace.</param>
    /// <param name="key">The metafield key.</param>
    /// <returns>The metafield, or null when the pair is not present.</returns>
    public static Metafield? GetMetafield<TValue>(
        this TValue entity,
        string? @namespace,
        string? key)
        where TValue : IMetafieldable
    {
        // Guard: Missing entity or inputs cannot match a metafield.
        if (entity is null || @namespace is null || key is null)
        {
            return null;
        }

        return entity.Metafields.FirstOrDefault(metafield =>
            metafield.Namespace == @namespace &&
            metafield.Key == key);
    }

    #endregion
}
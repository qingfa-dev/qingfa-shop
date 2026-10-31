namespace Core.BuildingBlocks.Modeling.Concerns.Content.Taggable;

/// <summary>Extension methods for the <see cref="ITaggable"/> concern.</summary>
public static class TaggableExtensions
{
    #region Public Methods

    /// <summary>
    /// Adds a tag to the entity. The tag is normalized (trimmed, lowercased)
    /// before validation runs, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the taggable entity.</typeparam>
    /// <param name="result">The result containing the taggable entity.</param>
    /// <param name="tag">The tag to add.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> AddTag<TValue>(
        this Result<TValue> result,
        string? tag)
        where TValue : ITaggable
    {
        var normalized = NormalizeTag(tag);

        return result
            .Bind(entity => TaggableValidator.ValidateAddTag(entity, normalized))
            .Tap(entity =>
            {
                entity.Tags.Add(normalized!);
            });
    }

    /// <summary>
    /// Removes a tag from the entity. The tag is normalized (trimmed, lowercased)
    /// before validation runs, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the taggable entity.</typeparam>
    /// <param name="result">The result containing the taggable entity.</param>
    /// <param name="tag">The tag to remove.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> RemoveTag<TValue>(
        this Result<TValue> result,
        string? tag)
        where TValue : ITaggable
    {
        var normalized = NormalizeTag(tag);

        return result
            .Bind(entity => TaggableValidator.ValidateRemoveTag(entity, normalized))
            .Tap(entity =>
            {
                entity.Tags.Remove(normalized!);
            });
    }

    /// <summary>
    /// Removes all tags from the entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the taggable entity.</typeparam>
    /// <param name="result">The result containing the taggable entity.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> ClearTags<TValue>(
        this Result<TValue> result)
        where TValue : ITaggable
    {
        return result
            .Bind(entity => TaggableValidator.ValidateClearTags(entity))
            .Tap(entity =>
            {
                entity.Tags.Clear();
            });
    }

    /// <summary>
    /// Normalizes a tag by trimming surrounding whitespace and lowercasing,
    /// per <see cref="TaggableConstant.Defaults.Normalization"/>.
    /// </summary>
    /// <param name="tag">The tag to normalize.</param>
    /// <returns>The normalized tag, or null when the input is null.</returns>
    public static string? NormalizeTag(string? tag)
    {
        // Guard: Preserve null for required-value validation downstream.
        if (tag is null)
        {
            return null;
        }

        // Compute: Apply configured normalization steps.
        var normalized = tag;

        if (TaggableConstant.Defaults.Normalization.TrimWhitespace)
        {
            normalized = normalized.Trim();
        }

        if (TaggableConstant.Defaults.Normalization.ToLowercase)
        {
            normalized = normalized.ToLowerInvariant();
        }

        return normalized;
    }

    #endregion
}
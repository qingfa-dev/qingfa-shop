using System.Text.RegularExpressions;

namespace Core.Common.Concerns.Content.Taggable;

/// <summary>Validator for the <see cref="ITaggable"/> concern.</summary>
public static class TaggableValidator
{
    #region Public Methods

    /// <summary>
    /// Validates the tag that <see cref="TaggableExtensions.AddTag{TValue}"/>
    /// is about to add, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the taggable entity.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="tag">The tag to add.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateAddTag<TValue>(
        TValue auditable,
        string? tag)
        where TValue : ITaggable
    {
        // Guard: Entity must not be null.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                TaggableOutcome.Failure.EntityRequired);
        }

        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(tag),
                error: TaggableOutcome.Failure.TagRequired)

            .Ensure(
                predicate: _ =>
                    tag!.Length >= TaggableConstant.Constraints.Tag.MinLength,
                error: TaggableOutcome.Failure.TagTooShort)

            .Ensure(
                predicate: _ =>
                    tag!.Length <= TaggableConstant.Constraints.Tag.MaxLength,
                error: TaggableOutcome.Failure.TagTooLong)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(tag!, TaggableConstant.Patterns.Tag),
                error: TaggableOutcome.Failure.TagInvalid)

            .Ensure(
                predicate: entity => !entity.Tags.Contains(tag!),
                error: TaggableOutcome.Failure.TagDuplicate)

            .Ensure(
                predicate: entity =>
                    entity.Tags.Count <
                    TaggableConstant.Constraints.Collection.MaxTags,
                error: TaggableOutcome.Failure.TagsTooMany);
    }

    /// <summary>
    /// Validates that the tag that
    /// <see cref="TaggableExtensions.RemoveTag{TValue}"/> is about to remove
    /// is present, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the taggable entity.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="tag">The tag to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateRemoveTag<TValue>(
        TValue auditable,
        string? tag)
        where TValue : ITaggable
    {
        // Guard: Entity must not be null.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                TaggableOutcome.Failure.EntityRequired);
        }

        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: entity => entity.Tags.Contains(tag!),
                error: TaggableOutcome.Failure.TagNotFound);
    }

    /// <summary>
    /// Validates that the entity that
    /// <see cref="TaggableExtensions.ClearTags{TValue}"/> is about to clear
    /// exists, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the taggable entity.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateClearTags<TValue>(
        TValue auditable)
        where TValue : ITaggable
    {
        // Guard: Entity must not be null.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                TaggableOutcome.Failure.EntityRequired);
        }

        return Result<TValue>.Success(auditable);
    }

    #endregion
}
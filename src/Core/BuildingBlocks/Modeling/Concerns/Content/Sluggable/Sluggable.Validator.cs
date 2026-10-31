using System.Text.RegularExpressions;

namespace Core.BuildingBlocks.Modeling.Concerns.Content.Sluggable;

/// <summary>Validator for the <see cref="ISluggable"/> concern.</summary>
public static class SluggableValidator
{
    #region Public Methods

    /// <summary>
    /// Validates the slug that <see cref="SluggableExtensions.SetSlug{TValue}"/>
    /// is about to apply, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the sluggable entity.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="slug">The slug to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateSlug<TValue>(
        TValue auditable,
        string? slug)
        where TValue : ISluggable
    {
        // Guard: Entity must not be null.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                SluggableOutcome.Failure.EntityRequired);
        }

        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(slug),
                error: SluggableOutcome.Failure.SlugRequired)

            .Ensure(
                predicate: _ =>
                    slug!.Length >= SluggableConstant.Constraints.Slug.MinLength,
                error: SluggableOutcome.Failure.SlugTooShort)

            .Ensure(
                predicate: _ =>
                    slug!.Length <= SluggableConstant.Constraints.Slug.MaxLength,
                error: SluggableOutcome.Failure.SlugTooLong)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(slug!, SluggableConstant.Patterns.Slug),
                error: SluggableOutcome.Failure.SlugInvalid);
    }

    #endregion
}
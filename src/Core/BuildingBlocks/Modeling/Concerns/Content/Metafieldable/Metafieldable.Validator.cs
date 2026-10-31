using System.Text.RegularExpressions;

namespace Core.BuildingBlocks.Modeling.Concerns.Content.Metafieldable;

/// <summary>Validator for the <see cref="IMetafieldable"/> concern.</summary>
public static class MetafieldableValidator
{
    #region Public Methods

    /// <summary>
    /// Validates the metafield that
    /// <see cref="MetafieldableExtensions.SetMetafield{TValue}(Result{TValue}, string, string, MetafieldType, string)"/>
    /// is about to apply, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the metafieldable entity.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="namespace">The metafield namespace.</param>
    /// <param name="key">The metafield key.</param>
    /// <param name="value">The metafield value (null is allowed).</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateMetafield<TValue>(
        TValue auditable,
        string? @namespace,
        string? key,
        string? value = null)
        where TValue : IMetafieldable
    {
        // Guard: Entity must not be null.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                MetafieldableOutcome.Failure.EntityRequired);
        }

        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(@namespace),
                error: MetafieldableOutcome.Failure.NamespaceRequired)

            .Ensure(
                predicate: _ =>
                    @namespace!.Length >=
                    MetafieldableConstant.Constraints.Namespace.MinLength,
                error: MetafieldableOutcome.Failure.NamespaceTooShort)

            .Ensure(
                predicate: _ =>
                    @namespace!.Length <=
                    MetafieldableConstant.Constraints.Namespace.MaxLength,
                error: MetafieldableOutcome.Failure.NamespaceTooLong)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(
                        @namespace!,
                        MetafieldableConstant.Patterns.SnakeCase),
                error: MetafieldableOutcome.Failure.NamespaceInvalid)

            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(key),
                error: MetafieldableOutcome.Failure.KeyRequired)

            .Ensure(
                predicate: _ =>
                    key!.Length >=
                    MetafieldableConstant.Constraints.Key.MinLength,
                error: MetafieldableOutcome.Failure.KeyTooShort)

            .Ensure(
                predicate: _ =>
                    key!.Length <=
                    MetafieldableConstant.Constraints.Key.MaxLength,
                error: MetafieldableOutcome.Failure.KeyTooLong)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(
                        key!,
                        MetafieldableConstant.Patterns.SnakeCase),
                error: MetafieldableOutcome.Failure.KeyInvalid)

            .Ensure(
                predicate: _ =>
                    value is null ||
                    value.Length <=
                    MetafieldableConstant.Constraints.Value.MaxLength,
                error: MetafieldableOutcome.Failure.ValueTooLong)

            .Ensure(
                predicate: entity =>
                    ContainsMetafield(entity, @namespace!, key!) ||
                    entity.Metafields.Count <
                    MetafieldableConstant.Constraints.Collection.MaxMetafields,
                error: MetafieldableOutcome.Failure.MetafieldsTooMany);
    }

    /// <summary>
    /// Validates that the metafield that
    /// <see cref="MetafieldableExtensions.RemoveMetafield{TValue}"/>
    /// targets is present, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the metafieldable entity.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="namespace">The metafield namespace.</param>
    /// <param name="key">The metafield key.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateMetafieldRemoval<TValue>(
        TValue auditable,
        string? @namespace,
        string? key)
        where TValue : IMetafieldable
    {
        // Guard: Entity must not be null.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                MetafieldableOutcome.Failure.EntityRequired);
        }

        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(@namespace),
                error: MetafieldableOutcome.Failure.NamespaceRequired)

            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(key),
                error: MetafieldableOutcome.Failure.KeyRequired)

            .Ensure(
                predicate: entity =>
                    ContainsMetafield(entity, @namespace!, key!),
                error: MetafieldableOutcome.Failure.MetafieldNotFound);
    }

    /// <summary>
    /// Validates that the entity that
    /// <see cref="MetafieldableExtensions.ClearMetafields{TValue}"/>
    /// is about to clear exists, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the metafieldable entity.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateClearMetafields<TValue>(
        TValue auditable)
        where TValue : IMetafieldable
    {
        // Guard: Entity must not be null.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                MetafieldableOutcome.Failure.EntityRequired);
        }

        return Result<TValue>.Success(auditable);
    }

    #endregion

    #region Private Methods

    /// <summary>Checks whether a (namespace, key) metafield already exists.</summary>
    private static bool ContainsMetafield<TValue>(
        TValue auditable,
        string @namespace,
        string key)
        where TValue : IMetafieldable
    {
        return auditable.Metafields.Any(metafield =>
            metafield.Namespace == @namespace &&
            metafield.Key == key);
    }

    #endregion
}
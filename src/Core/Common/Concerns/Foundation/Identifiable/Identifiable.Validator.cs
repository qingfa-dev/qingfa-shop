namespace Core.Common.Concerns.Foundation.Identifiable;

/// <summary>Validates identification state for <see cref="IIdentifiable{TKey}"/> entities.</summary>
public static class IdentifiableValidator
{
    /// <summary>Validates that the entity carries a non-default identifier.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TKey">The type of the identifier.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <returns>Success if identified; otherwise a failure with the appropriate error.</returns>
    public static Result<TValue> ValidateIdentification<TValue, TKey>(
        TValue auditable)
        where TValue : IIdentifiable<TKey>
    {
        // Guard: Reject null entities immediately.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                IdentifiableOutcome.Failure.EntityRequired);
        }

        // Compute: Check that the identifier is not the default value for its type.
        // Fallback: Reject Guid.Empty explicitly and blank strings, which a bare
        // default-value comparison does not always cover (string.Empty != default(string)).
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: entity =>
                    !EqualityComparer<TKey>.Default.Equals(
                        entity.Id,
                        default!)
                    && !IsEmptyId(entity.Id),
                error: IdentifiableOutcome.Failure.IdRequired);
    }

    /// <summary>Validates a candidate identifier value without an entity.</summary>
    /// <typeparam name="TKey">The type of the identifier.</typeparam>
    /// <param name="id">The identifier value to validate.</param>
    /// <returns>Success if usable; otherwise a failure with the appropriate error.</returns>
    public static Result<TKey> ValidateId<TKey>(
        TKey id)
    {
        // Guard: Reject null identifiers immediately.
        if (id is null)
        {
            return Result<TKey>.Fail(
                IdentifiableOutcome.Failure.IdRequired);
        }

        // Compute: Check the candidate is neither the default nor an empty sentinel.
        return Result<TKey>.Success(id)
            .Ensure(
                predicate: candidate =>
                    !EqualityComparer<TKey>.Default.Equals(
                        candidate,
                        default!)
                    && !IsEmptyId(candidate),
                error: IdentifiableOutcome.Failure.IdRequired);
    }

    /// <summary>Checks <see cref="Guid.Empty"/> and blank-string sentinels for the identifier.</summary>
    /// <typeparam name="TKey">The type of the identifier.</typeparam>
    /// <param name="id">The identifier value to inspect.</param>
    /// <returns><c>true</c> when the value is an empty sentinel; otherwise <c>false</c>.</returns>
    private static bool IsEmptyId<TKey>(
        TKey id) =>
        (id is Guid guid && guid == IdentifiableConstant.Defaults.EmptyId)
        || (id is string text && string.IsNullOrWhiteSpace(text));
}
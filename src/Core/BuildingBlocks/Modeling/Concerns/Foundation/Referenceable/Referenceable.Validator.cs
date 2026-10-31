using System.Text.RegularExpressions;

namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.Referenceable;

/// <summary>Validates reference state for <see cref="IReferenceable"/> entities.</summary>
public static class ReferenceableValidator
{
    /// <summary>Validates the reference that <see cref="ReferenceableExtensions.SetReference{TValue}"/>
    /// is about to apply, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="reference">The reference value to validate.</param>
    /// <returns>Success if valid; otherwise a failure with the appropriate error.</returns>
    public static Result<TValue> ValidateReference<TValue>(
        TValue auditable,
        string? reference)
        where TValue : IReferenceable
    {
        // Guard: Reject null entities immediately.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                ReferenceableOutcome.Failure.EntityRequired);
        }

        // Compute: Validate the reference is not null or whitespace.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(reference),
                error: ReferenceableOutcome.Failure.ReferenceRequired)

            // Compute: Validate the reference length does not exceed the limit.
            .Ensure(
                predicate: _ =>
                    reference!.Length <=
                    ReferenceableConstant.Constraints.Reference.MaxLength,
                error: ReferenceableOutcome.Failure.ReferenceTooLong)

            // Compute: Validate the reference has no leading or trailing whitespace.
            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(
                        reference!,
                        ReferenceableConstant.Patterns.Trimmed),
                error: ReferenceableOutcome.Failure.ReferenceUntrimmed)

            // Compute: Validate the reference uses only the allowed charset.
            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(
                        reference!,
                        ReferenceableConstant.Patterns.AllowedCharset),
                error: ReferenceableOutcome.Failure.ReferenceInvalidCharset);
    }
}
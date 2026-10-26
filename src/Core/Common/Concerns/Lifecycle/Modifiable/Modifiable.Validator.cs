using System.Text.RegularExpressions;

using Core.Common.Concerns.Lifecycle.Creatable;

namespace Core.Common.Concerns.Lifecycle.Modifiable;

/// <summary>Validates modification operations for <see cref="ICreatable"/> and <see cref="IModifiable"/> entities.</summary>
public static class ModifiableValidator
{
    /// <summary>
    /// Validates the audit metadata that <see cref="ModifiableExtensions.MarkModified{TValue}"/>
    /// is about to apply, without mutating the entity.
    /// </summary>
    public static Result<TValue> ValidateModification<TValue>(
        TValue auditable,
        DateTimeOffset nowUtc,
        string? actor,
        bool requireModifiedBy = LifecycleConstant.Defaults.RequireActor)
        where TValue : ICreatable, IModifiable
    {
        // Guard: Null entity check.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                ModifiableOutcome.Failure.EntityRequired);
        }

        // Compute: Chain validation checks in priority order.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ => nowUtc != default,
                error: ModifiableOutcome.Failure.ModifiedAtRequired)

            .Ensure(
                predicate: _ => nowUtc <= DateTimeOffset.UtcNow.AddSeconds(
                    LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds),
                error: ModifiableOutcome.Failure.ModifiedAtInFuture)

            .Ensure(
                predicate: entity => entity.CreatedAtUtc != default,
                error: ModifiableOutcome.Failure.NotInitialized)

            .Ensure(
                predicate: entity => nowUtc >= entity.CreatedAtUtc,
                error: ModifiableOutcome.Failure.ModifiedAtBeforeCreatedAt)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    actor.Length <=
                    LifecycleConstant.Constraints.Actor.MaxLength,
                error: ModifiableOutcome.Failure.ModifiedByTooLong)

            .Ensure(
                predicate: _ =>
                    !requireModifiedBy ||
                    !string.IsNullOrWhiteSpace(actor),
                error: ModifiableOutcome.Failure.ModifiedByRequired)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    Regex.IsMatch(actor, LifecycleConstant.Patterns.Actor),
                error: ModifiableOutcome.Failure.ActorInvalid)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    actor.Length >=
                    LifecycleConstant.Constraints.Actor.MinLength,
                error: ModifiableOutcome.Failure.ModifiedByTooShort);
    }
}
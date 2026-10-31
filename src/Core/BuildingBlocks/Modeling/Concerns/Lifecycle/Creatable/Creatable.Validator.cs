using System.Text.RegularExpressions;

namespace Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Creatable;

/// <summary>Validates creation operations for <see cref="ICreatable"/> entities.</summary>
public static class CreatableValidator
{
    /// <summary>
    /// Validates the audit metadata that <see cref="CreatableExtensions.InitializeAudit{TValue}"/>
    /// is about to apply, without mutating the entity.
    /// </summary>
    public static Result<TValue> ValidateCreation<TValue>(
        TValue auditable,
        DateTimeOffset nowUtc,
        string? actor,
        bool requireCreatedBy = LifecycleConstant.Defaults.RequireActor)
        where TValue : ICreatable
    {
        // Guard: Null entity check.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                CreatableOutcome.Failure.EntityRequired);
        }

        // Compute: Chain validation checks in priority order.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ => nowUtc != default,
                error: CreatableOutcome.Failure.CreatedAtRequired)

            .Ensure(
                predicate: _ => nowUtc <= DateTimeOffset.UtcNow.AddSeconds(
                    LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds),
                error: CreatableOutcome.Failure.CreatedAtInFuture)

            .Ensure(
                predicate: entity => entity.CreatedAtUtc == default,
                error: CreatableOutcome.Failure.AlreadyInitialized)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    actor.Length <=
                    LifecycleConstant.Constraints.Actor.MaxLength,
                error: CreatableOutcome.Failure.CreatedByTooLong)

            .Ensure(
                predicate: _ =>
                    !requireCreatedBy ||
                    !string.IsNullOrWhiteSpace(actor),
                error: CreatableOutcome.Failure.CreatedByRequired)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    Regex.IsMatch(actor, LifecycleConstant.Patterns.Actor),
                error: CreatableOutcome.Failure.ActorInvalid)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    actor.Length >=
                    LifecycleConstant.Constraints.Actor.MinLength,
                error: CreatableOutcome.Failure.CreatedByTooShort);
    }
}
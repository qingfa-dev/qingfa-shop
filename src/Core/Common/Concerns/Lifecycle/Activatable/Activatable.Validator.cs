using System.Text.RegularExpressions;

namespace Core.Common.Concerns.Lifecycle.Activatable;

/// <summary>Validates activation and deactivation operations for <see cref="IActivatable"/> entities.</summary>
public static class ActivatableValidator
{
    /// <summary>
    /// Validates the activation metadata that
    /// <see cref="ActivatableExtensions.Activate{TValue}"/> is about to apply,
    /// without mutating the entity.
    /// </summary>
    public static Result<TValue> ValidateActivation<TValue>(
        TValue auditable,
        DateTimeOffset nowUtc,
        string? actor,
        bool requireActor = LifecycleConstant.Defaults.RequireActor)
        where TValue : IActivatable
    {
        // Guard: Null entity check.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                ActivatableOutcome.Failure.EntityRequired);
        }

        // Compute: Chain validation checks in priority order.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ => nowUtc != default,
                error: ActivatableOutcome.Failure.ActivatedAtRequired)

            .Ensure(
                predicate: _ => nowUtc <= DateTimeOffset.UtcNow.AddSeconds(
                    LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds),
                error: ActivatableOutcome.Failure.ActivatedAtInFuture)

            .Ensure(
                predicate: entity => !entity.IsActive,
                error: ActivatableOutcome.Failure.AlreadyActive)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    actor.Length <=
                    LifecycleConstant.Constraints.Actor.MaxLength,
                error: ActivatableOutcome.Failure.ActivatedByTooLong)

            .Ensure(
                predicate: _ =>
                    !requireActor ||
                    !string.IsNullOrWhiteSpace(actor),
                error: ActivatableOutcome.Failure.ActivatedByRequired)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    Regex.IsMatch(actor, LifecycleConstant.Patterns.Actor),
                error: ActivatableOutcome.Failure.ActorInvalid)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    actor.Length >=
                    LifecycleConstant.Constraints.Actor.MinLength,
                error: ActivatableOutcome.Failure.ActivatedByTooShort);
    }

    /// <summary>
    /// Validates the deactivation metadata that
    /// <see cref="ActivatableExtensions.Deactivate{TValue}"/> is about to apply,
    /// without mutating the entity.
    /// </summary>
    public static Result<TValue> ValidateDeactivation<TValue>(
        TValue auditable,
        string? actor,
        bool requireActor = LifecycleConstant.Defaults.RequireActor)
        where TValue : IActivatable
    {
        // Guard: Null entity check.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                ActivatableOutcome.Failure.EntityRequired);
        }

        // Compute: Chain validation checks in priority order.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: entity => entity.IsActive,
                error: ActivatableOutcome.Failure.NotActive)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    actor.Length <=
                    LifecycleConstant.Constraints.Actor.MaxLength,
                error: ActivatableOutcome.Failure.ActivatedByTooLong)

            .Ensure(
                predicate: _ =>
                    !requireActor ||
                    !string.IsNullOrWhiteSpace(actor),
                error: ActivatableOutcome.Failure.ActivatedByRequired)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    Regex.IsMatch(actor, LifecycleConstant.Patterns.Actor),
                error: ActivatableOutcome.Failure.ActorInvalid)

            .Ensure(
                predicate: _ =>
                    actor is null ||
                    actor.Length >=
                    LifecycleConstant.Constraints.Actor.MinLength,
                error: ActivatableOutcome.Failure.ActivatedByTooShort);
    }
}
namespace Application.Common.Persistence.Initialization.Resetters;

using Core.Common.Monad.Results;

/// <summary>
/// Guard utilities for validating resetter-related values against <see cref="ResetterConstant"/> limits.
/// </summary>
/// <remarks>
/// Returns <see cref="Result"/> instead of throwing, enabling monadic validation composition.
/// </remarks>
public static class ResetterGuard
{
    #region Resetter Validation

    /// <summary>
    /// Validates a resetter order.
    /// </summary>
    /// <param name="order">The resetter order to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateOrder(
        int order,
        string parameterName = "order")
    {
        if (order < 0)
        {
            return Result.Fail(ResetterOutcome.Failure.OrderNegative(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates the number of registered resetters.
    /// </summary>
    /// <param name="count">The number of resetters.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateCount(
        int count,
        string parameterName = "count")
    {
        if (count < 0)
        {
            return Result.Fail(ResetterOutcome.Failure.CountNegative(parameterName));
        }

        if (count > ResetterConstant.Limits.MaxResetters)
        {
            return Result.Fail(ResetterOutcome.Failure.CountExceedsMax(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates a reset scope.
    /// </summary>
    /// <param name="scope">The reset scope to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateScope(
        string? scope,
        string parameterName = "scope")
    {
        if (scope is null)
        {
            return Result.Fail(ResetterOutcome.Failure.ScopeNull(parameterName));
        }

        var validScopes = new[] { ResetterConstant.Scope.Data, ResetterConstant.Scope.Schema, ResetterConstant.Scope.All };
        if (!validScopes.Contains(scope))
        {
            return Result.Fail(ResetterOutcome.Failure.ScopeInvalid(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates all resetter properties at once.
    /// </summary>
    /// <param name="order">The resetter order.</param>
    /// <param name="scope">The reset scope.</param>
    /// <returns>A successful Result if all valid; otherwise a failed Result with combined errors.</returns>
    public static Result ValidateResetter(
        int order,
        string? scope)
    {
        var results = new List<Result>
        {
            ValidateOrder(order),
            ValidateScope(scope)
        };

        return ResultExtension.Combine([.. results]);
    }

    #endregion
}
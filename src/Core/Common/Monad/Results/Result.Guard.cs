namespace Core.Common.Monad.Results;

/// <summary>
/// Guard utilities for validating <see cref="Result"/> construction and mutation.
/// </summary>
/// <remarks>
/// Centralizes validation logic so that <see cref="Result"/> constructors
/// and factory methods share consistent constraints and error messages.
/// </remarks>
public static class ResultGuard
{
    #region Status

    /// <summary>
    /// Validates an HTTP status code against success/failure constraints.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="isSuccess">Whether the result represents success.</param>
    /// <returns>An error if validation fails; otherwise <c>null</c>.</returns>
    public static Error? ValidateStatusCode(
        int statusCode,
        bool isSuccess)
    {
        if (statusCode is
            < ResultConstant.Constraint.Status.Min or
            > ResultConstant.Constraint.Status.Max)
        {
            return ResultOutcome.Failure.Status.InvalidStatusCode(statusCode);
        }

        if (isSuccess)
        {
            return statusCode is
                < ResultConstant.Constraint.Status.Success.Min or
                > ResultConstant.Constraint.Status.Success.Max
                    ? ResultOutcome.Failure.Status.InvalidSuccessStatus(statusCode)
                    : null;
        }

        return statusCode is
            < ResultConstant.Constraint.Status.Failure.Min or
            > ResultConstant.Constraint.Status.Failure.Max
                ? ResultOutcome.Failure.Status.InvalidFailureStatus(statusCode)
                : null;
    }

    #endregion

    #region Errors

    /// <summary>
    /// Validates the error list against success/failure constraints.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <param name="isSuccess">Whether the result represents success.</param>
    /// <returns>An error if validation fails; otherwise <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    public static Error? ValidateErrors(
        List<Error> errors,
        bool isSuccess)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count > ResultConstant.Constraint.Errors.MaxCount)
        {
            return ResultOutcome.Failure.Errors.ExceedsMaxCount(
                ResultConstant.Constraint.Errors.MaxCount);
        }

        if (!isSuccess && errors.Count == 0)
        {
            return ResultOutcome.Failure.Errors.Empty;
        }

        return null;
    }

    #endregion

    #region Value

    /// <summary>
    /// Validates that a successful result has a value.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="isSuccess">Whether the result represents success.</param>
    /// <param name="value">The value.</param>
    /// <returns>An error if validation fails; otherwise <c>null</c>.</returns>
    public static Error? ValidateValue<TValue>(
        bool isSuccess,
        TValue? value)
    {
        if (isSuccess && value is null)
        {
            return ResultOutcome.Failure.Value.Required;
        }

        return null;
    }

    #endregion

    #region Result

    /// <summary>
    /// Validates that all errors in a failure result have the same status code.
    /// </summary>
    /// <param name="isSuccess">Whether the result represents success.</param>
    /// <param name="errors">The list of errors.</param>
    /// <returns>An error if validation fails; otherwise <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    public static Error? ValidateResultConsistency(
        bool isSuccess,
        List<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (isSuccess || errors.Count <= 1)
        {
            return null;
        }

        var status = errors[0].Status;

        for (var index = 1; index < errors.Count; index++)
        {
            if (errors[index].Status != status)
            {
                return ResultOutcome.Failure.Errors.MixedStatusCodes;
            }
        }

        return null;
    }

    #endregion
}
namespace Application.BuildingBlocks.Persistence.Initialization.Seeders;
/// <summary>
/// Guard utilities for validating seeder-related values against <see cref="SeederConstant"/> limits.
/// </summary>
/// <remarks>
/// Returns <see cref="Result"/> instead of throwing, enabling monadic validation composition.
/// </remarks>
public static class SeederGuard
{
    #region Seeder Validation

    /// <summary>
    /// Validates a seeder order.
    /// </summary>
    /// <param name="order">The seeder order to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateOrder(
        int order,
        string parameterName = "order")
    {
        if (order < 0)
        {
            return Result.Fail(SeederOutcome.Failure.OrderNegative(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates the number of registered seeders.
    /// </summary>
    /// <param name="count">The number of seeders.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateCount(
        int count,
        string parameterName = "count")
    {
        if (count < 0)
        {
            return Result.Fail(SeederOutcome.Failure.CountNegative(parameterName));
        }

        if (count > SeederConstant.Limits.MaxSeeders)
        {
            return Result.Fail(SeederOutcome.Failure.CountExceedsMax(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates a batch size for seeding operations.
    /// </summary>
    /// <param name="batchSize">The batch size to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateBatchSize(
        int batchSize,
        string parameterName = "batchSize")
    {
        if (batchSize < SeederConstant.Limits.MinBatchSize)
        {
            return Result.Fail(SeederOutcome.Failure.BatchSizeTooSmall(parameterName));
        }

        if (batchSize > SeederConstant.Limits.MaxBatchSize)
        {
            return Result.Fail(SeederOutcome.Failure.BatchSizeTooLarge(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates all seeder properties at once.
    /// </summary>
    /// <param name="order">The seeder order.</param>
    /// <param name="batchSize">The batch size.</param>
    /// <returns>A successful Result if all valid; otherwise a failed Result with combined errors.</returns>
    public static Result ValidateSeeder(
        int order,
        int batchSize)
    {
        var results = new List<Result>
        {
            ValidateOrder(order),
            ValidateBatchSize(batchSize)
        };

        return ResultExtension.Combine([.. results]);
    }

    #endregion
}
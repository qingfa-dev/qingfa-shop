namespace Core.BuildingBlocks.Functional.Results;

public static partial class ResultExtension
{
    #region Combination

    public static Result Combine(params Result[] results)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (results.Length == 0)
            return Result.Ok();

        foreach (var result in results)
        {
            if (result is null)
            {
                return Result.Fail(
                    ResultOutcome.Failure.Flow.ResultMissing(nameof(Combine)));
            }
        }

        var failures = results
            .Where(result => result.IsFailure)
            .ToList();

        if (failures.Count == 0)
            return Result.Ok();

        var statusCode = failures[0].StatusCode;

        if (failures.Any(result => result.StatusCode != statusCode))
        {
            return Result.Fail(ResultOutcome.Failure.Errors.MixedStatusCodes);
        }

        var errors = failures.SelectMany(result => result.Errors).ToList();

        return Result.Failure(errors, statusCode);
    }

    #endregion
}

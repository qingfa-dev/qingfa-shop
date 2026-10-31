namespace Core.BuildingBlocks.Functional.Results;

public static partial class ResultExtension
{
    /// <summary>Drops the success value while preserving failure errors and metadata.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>A value-less result with the same outcome.</returns>
    public static Result ToResult<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? Result.Success().WithMetadataFrom(result)
            : Result.Fail([.. result.Errors], result.StatusCode).WithMetadataFrom(result);
    }
}

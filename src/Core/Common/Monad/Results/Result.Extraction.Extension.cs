namespace Core.Common.Monad.Results;

public static partial class ResultExtension
{
    #region Extraction

    public static TValue? ValueOr<TValue>(
        this Result<TValue> result,
        TValue? fallback = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? result.Value : fallback;
    }

    public static TValue ValueOrThrow<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        result.ThrowIfFailure();

        return result.Value;
    }

    public static Result ThrowIfFailure(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
            throw new ResultException(result);

        return result;
    }

    public static Result<TValue> ThrowIfFailure<TValue>(
        this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
            throw new ResultException(result);

        return result;
    }

    #endregion
}

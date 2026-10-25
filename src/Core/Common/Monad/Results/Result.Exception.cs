namespace Core.Common.Monad.Results;

public sealed class ResultException : Exception
{
    #region Properties

    public Result Result { get; }

    public IReadOnlyList<Error> Errors
        => Result.Errors;

    public int StatusCode
        => Result.StatusCode;

    #endregion

    #region Constructor

    public ResultException(
        Result result)
        : base(CreateMessage(result))
    {
        ArgumentNullException.ThrowIfNull(result);

        Result = result;
    }

    #endregion

    #region Helpers

    private static string CreateMessage(
        Result result)
        => result.Errors.Count == 0
            ? $"Result failed with status {result.StatusCode}."
            : string.Join(
                Environment.NewLine,
                result.Errors.Select(error => error.ToString()));

    #endregion
}
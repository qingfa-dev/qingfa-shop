namespace Core.BuildingBlocks.Functional.Results;

public partial record Result
{
    #region Factory

    #region Custom

    /// <summary>
    /// Creates a custom <see cref="Result"/> with full control over all members.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="errors">The errors (optional for success, required for failure).</param>
    /// <param name="statusCode">The HTTP status code (optional).</param>
    /// <returns>A new <see cref="Result"/> instance.</returns>
    public static Result Custom(
        bool isSuccess,
        List<Error>? errors = null,
        int? statusCode = null)
        => new(
            isSuccess,
            errors?.ToList(),
            statusCode);

    #endregion

    #region Success

    /// <summary>
    /// Creates a successful result with the specified status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default 200).</param>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success(
        int statusCode = ResultConstant.StatusCode.Ok)
        => new(
            isSuccess: true,
            statusCode: statusCode);

    /// <summary>
    /// Creates a successful result with status 200 OK.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Ok()
        => Success(ResultConstant.StatusCode.Ok);

    /// <summary>
    /// Creates a successful result with status 201 Created.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Created()
        => Success(ResultConstant.StatusCode.Created);

    /// <summary>
    /// Creates a successful result with status 202 Accepted.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Accepted()
        => Success(ResultConstant.StatusCode.Accepted);

    /// <summary>
    /// Creates a successful result with status 204 No Content.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result NoContent()
        => Success(ResultConstant.StatusCode.NoContent);

    #endregion

    #region Failure

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    public static Result Failure(
        List<Error> errors)
        => Fail(errors);

    /// <summary>
    /// Creates a failed result with the specified errors and status code.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    public static Result Failure(
        List<Error> errors,
        int statusCode)
        => Fail(errors, statusCode);

    /// <summary>
    /// Creates a failed result from the specified errors.
    /// </summary>
    /// <param name="errors">The errors.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Fail(
        params Error[] errors)
        => Fail(errors.ToList());

    /// <summary>
    /// Creates a failed result from the specified error list.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    public static Result Fail(
        List<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return new Result(
            isSuccess: false,
            errors: errors.ToList());
    }

    /// <summary>
    /// Creates a failed result from the specified error list and status code.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    public static Result Fail(
        List<Error> errors,
        int statusCode)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return new Result(
            isSuccess: false,
            errors: errors,
            statusCode: statusCode);
    }

    #endregion

    #endregion
}
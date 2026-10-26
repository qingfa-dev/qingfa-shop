namespace Core.Common.Monad.Results;

public partial record Result<TValue>
{
    #region Factory
    #region Custom

    /// <summary>
    /// Creates a custom <see cref="Result{TValue}"/> with full control over all members.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="value">The value.</param>
    /// <param name="errors">The errors (optional for success, required for failure).</param>
    /// <param name="statusCode">The HTTP status code (optional).</param>
    /// <returns>A new <see cref="Result{TValue}"/> instance.</returns>
    public static Result<TValue> Custom(
        bool isSuccess,
        TValue value,
        List<Error>? errors = null,
        int? statusCode = null)
        => new(
            isSuccess: isSuccess,
            value: value,
            errors: errors?.ToList(),
            statusCode: statusCode);

    #endregion
    #region Success

    /// <summary>
    /// Creates a successful result with the specified value and status code.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <param name="statusCode">The HTTP status code (default 200).</param>
    /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Ok(
        TValue value,
        int statusCode = ResultConstant.StatusCode.Ok)
        => new(
            isSuccess: true,
            value: value,
            statusCode: statusCode);

    /// <summary>
    /// Creates a successful result with the specified value and status code.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <param name="statusCode">The HTTP status code (default 200).</param>
    /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Success(
        TValue value,
        int statusCode = ResultConstant.StatusCode.Ok)
        => Ok(value, statusCode);

    /// <summary>
    /// Creates a successful result with status 201 Created.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Created(
        TValue value)
        => Ok(
            value,
            ResultConstant.StatusCode.Created);

    /// <summary>
    /// Creates a successful result with status 202 Accepted.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Accepted(
        TValue value)
        => Ok(
            value,
            ResultConstant.StatusCode.Accepted);

    #endregion

    #region Failure

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    public static new Result<TValue> Failure(
        List<Error> errors)
        => Fail(errors);

    /// <summary>
    /// Creates a failed result with the specified errors and status code.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    public static new Result<TValue> Failure(
        List<Error> errors,
        int statusCode)
        => Fail(errors, statusCode);

    /// <summary>
    /// Creates a failed result from the specified errors.
    /// </summary>
    /// <param name="errors">The errors.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    public static new Result<TValue> Fail(
        params Error[] errors)
        => Fail(errors.ToList());

    /// <summary>
    /// Creates a failed result from the specified error list.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    public static new Result<TValue> Fail(
        List<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return new Result<TValue>(
            isSuccess: false,
            value: default!,
            errors: errors.ToList());
    }

    /// <summary>
    /// Creates a failed result from the specified error list and status code.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    public static new Result<TValue> Fail(
        List<Error> errors,
        int statusCode)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return new Result<TValue>(
            isSuccess: false,
            value: default!,
            errors: errors.ToList(),
            statusCode: statusCode);
    }

    #endregion

    #endregion
}
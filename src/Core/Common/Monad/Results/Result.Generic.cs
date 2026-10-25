using System.Diagnostics.CodeAnalysis;

namespace Core.Common.Monad.Results;

/// <summary>
/// Represents the outcome of an operation that can succeed with a value or fail.
/// </summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Result{TValue}"/> models operations that either complete successfully
/// with a value of type <typeparamref name="TValue"/> or fail with a list of
/// <see cref="Error"/> objects. It carries an HTTP status code for easy
/// conversion to HTTP responses and a metadata dictionary for contextual information.
/// </para>
/// <para>
/// This is the primary result type for operations that return data.
/// Use the non-generic <see cref="Result"/> for operations without a return value.
/// </para>
/// </remarks>
public partial record Result<TValue>
    : Result,
      IResult<TValue, Error>
{
    #region Value

    /// <summary>
    /// Gets the value when the operation succeeded.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The result is a failure (use <see cref="TryGetValue"/> for safe access).
    /// </exception>
    [AllowNull]
    public TValue Value { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new result with a value.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="value">The value (required for success).</param>
    /// <param name="errors">The errors (required for failure).</param>
    /// <param name="statusCode">The HTTP status code (optional).</param>
    /// <exception cref="ArgumentException">
    /// Validation fails: success with null value, failure with value,
    /// invalid status code, or mixed error status codes.
    /// </exception>
    public Result(
        bool isSuccess,
        TValue value,
        List<Error>? errors = null,
        int? statusCode = null)
        : base(
            isSuccess: isSuccess,
            errors: errors ?? new List<Error>(),
            statusCode: statusCode)
    {
        var valueError = ResultGuard.ValidateValue(
            isSuccess,
            value);

        if (valueError is not null)
        {
            throw new ArgumentException(
                valueError.ToString(),
                nameof(value));
        }

        Value = value;
    }

    #endregion

    #region Copy

    /// <summary>
    /// Creates a copy of the current result.
    /// </summary>
    /// <param name="isSuccess">
    /// The replacement success flag; the current value is retained when <c>null</c>.
    /// </param>
    /// <param name="errors">
    /// The replacement errors; the current value is retained when <c>null</c>
    /// and <paramref name="isSuccess"/> is unchanged.
    /// </param>
    /// <param name="statusCode">
    /// The replacement status code; the current value is retained when <c>null</c>.
    /// </param>
    /// <param name="metadata">
    /// The replacement metadata; the current value is retained when <c>null</c>.
    /// </param>
    /// <returns>A new result containing the requested overrides.</returns>
    /// <exception cref="ArgumentException">
    /// Cannot change a failed result to success without providing a value.
    /// </exception>
    /// <remarks>
    /// A failed result cannot become successful without supplying a value.
    /// </remarks>
    public new Result<TValue> CopyWith(
        bool? isSuccess = null,
        List<Error>? errors = null,
        int? statusCode = null,
        DefaultMetadata? metadata = null)
    {
        var resolvedIsSuccess = isSuccess ?? IsSuccess;

        if (resolvedIsSuccess && !IsSuccess)
        {
            throw new ArgumentException(
                ResultOutcome.Failure.Value.Required.ToString(),
                nameof(isSuccess));
        }

        List<Error> resolvedErrors = errors is not null
            ? errors.ToList()
            : Errors.ToList();

        var resolvedStatusCode = statusCode
            ?? (resolvedIsSuccess == IsSuccess
                ? StatusCode
                : Result.ResolveDefaultStatusCode(
                    resolvedIsSuccess,
                    resolvedErrors));

        return new Result<TValue>(
            isSuccess: resolvedIsSuccess,
            value: Value,
            errors: resolvedErrors,
            statusCode: resolvedStatusCode)
        {
            Metadata = DefaultMetadata.Create(
                metadata ?? Metadata)
        };
    }

    /// <summary>
    /// Creates a copy of the current result with the specified value.
    /// </summary>
    /// <param name="value">The replacement value.</param>
    /// <param name="isSuccess">
    /// The replacement success flag; the current value is retained when <c>null</c>.
    /// </param>
    /// <param name="errors">
    /// The replacement errors; the current value is retained when <c>null</c>.
    /// </param>
    /// <param name="statusCode">
    /// The replacement status code; the current value is retained when <c>null</c>.
    /// </param>
    /// <param name="metadata">
    /// The replacement metadata; the current value is retained when <c>null</c>.
    /// </param>
    /// <returns>A new result containing the requested overrides.</returns>
    public Result<TValue> CopyWith(
        TValue value,
        bool? isSuccess = null,
        List<Error>? errors = null,
        int? statusCode = null,
        DefaultMetadata? metadata = null)
    {
        var resolvedIsSuccess = isSuccess ?? IsSuccess;

        List<Error> resolvedErrors = errors is not null
            ? [.. errors]
            : resolvedIsSuccess
                ? new List<Error>()
                : [.. Errors];

        var resolvedStatusCode = statusCode
            ?? (resolvedIsSuccess == IsSuccess
                ? StatusCode
                : Result.ResolveDefaultStatusCode(
                    resolvedIsSuccess,
                    resolvedErrors));

        return new Result<TValue>(
            isSuccess: resolvedIsSuccess,
            value: value,
            errors: resolvedErrors,
            statusCode: resolvedStatusCode)
        {
            Metadata = DefaultMetadata.Create(
                metadata ?? Metadata)
        };
    }

    #endregion

    #region Value Access

    /// <summary>
    /// Attempts to get the value when the result is successful.
    /// </summary>
    /// <param name="value">The value if successful; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if the result is successful; otherwise <c>false</c>.</returns>
    public bool TryGetValue(
        [MaybeNullWhen(false)] out TValue value)
    {
        if (IsSuccess)
        {
            value = Value;
            return true;
        }

        value = default;
        return false;
    }

    #endregion

    #region ToString

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns>
    /// <c>"Success (StatusCode): Value"</c> or <c>"Failure (StatusCode): Error1, Error2"</c>.
    /// </returns>
    public override string ToString()
        => IsSuccess
            ? $"Success ({StatusCode}): {Value}"
            : $"Failure ({StatusCode}): {string.Join(", ", Errors)}";

    #endregion
}
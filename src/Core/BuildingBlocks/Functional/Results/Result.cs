using System.Text.Json.Serialization;

namespace Core.BuildingBlocks.Functional.Results;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail without a return value.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Result"/> models operations that either complete successfully or fail with
/// a list of <see cref="Error"/> objects. It carries an HTTP status code for easy
/// conversion to HTTP responses and a metadata dictionary for contextual information.
/// </para>
/// <para>
/// Use <see cref="Result{TValue}"/> when the operation returns a value on success.
/// </para>
/// </remarks>
public partial record Result : IResult<Error>
{
    #region Properties

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets a value indicating whether the result has a value (always <c>true</c> for success).
    /// </summary>
    [JsonIgnore]
    public bool HasValue => IsSuccess;

    /// <summary>
    /// Gets the list of errors when the operation failed.
    /// </summary>
    /// <remarks>
    /// Empty when <see cref="IsSuccess"/> is <c>true</c>.
    /// </remarks>
    public List<Error> Errors { get; }

    /// <summary>
    /// Gets the metadata dictionary for contextual information.
    /// </summary>
    public DefaultMetadata Metadata { get; protected set; } = new();

    /// <summary>
    /// Gets the HTTP status code associated with the result.
    /// </summary>
    /// <remarks>
    /// For success: 2xx status code (default 200).
    /// For failure: 4xx or 5xx status code derived from the first error,
    /// or 500 if no error has a status.
    /// </remarks>
    public int StatusCode { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new result.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="errors">The errors (required for failure).</param>
    /// <param name="statusCode">The HTTP status code (optional).</param>
    /// <exception cref="ArgumentException">
    /// Validation fails: success with errors, failure without errors,
    /// invalid status code, or mixed error status codes.
    /// </exception>
    public Result(
        bool isSuccess,
        List<Error>? errors = null,
        int? statusCode = null)
    {
        var resolvedErrors = errors ?? ResultConstant.Default.EmptyErrors;

        var resolvedStatusCode =
            statusCode
            ?? ResolveDefaultStatusCode(isSuccess, resolvedErrors);

        var error =
            ResultGuard.ValidateStatusCode(
                resolvedStatusCode,
                isSuccess)
            ?? ResultGuard.ValidateErrors(
                resolvedErrors,
                isSuccess)
            ?? ResultGuard.ValidateResultConsistency(
                isSuccess,
                resolvedErrors);

        if (error is not null)
        {
            throw new ArgumentException(
                error.ToString(),
                nameof(errors));
        }

        IsSuccess = isSuccess;
        Errors = resolvedErrors.ToList();
        StatusCode = resolvedStatusCode;
    }

    #endregion

    #region Copy

    /// <summary>
    /// Creates a copy of this result with the specified members overridden.
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
    public Result CopyWith(
        bool? isSuccess = null,
        List<Error>? errors = null,
        int? statusCode = null,
        DefaultMetadata? metadata = null)
    {
        var resolvedIsSuccess = isSuccess ?? IsSuccess;

        var resolvedErrors = errors is not null
            ? errors.ToList()
            : resolvedIsSuccess == IsSuccess
                ? Errors.ToList()
                : [];

        var resolvedStatusCode =
            statusCode
            ?? ResolveDefaultStatusCode(
                resolvedIsSuccess,
                resolvedErrors);

        return new Result(
            isSuccess: resolvedIsSuccess,
            errors: resolvedErrors,
            statusCode: resolvedStatusCode)
        {
            Metadata = DefaultMetadata.Create(
                metadata ?? Metadata)
        };
    }

    #endregion

    #region Equality

    /// <summary>
    /// Compares this result with another result using the result value members.
    /// </summary>
    /// <param name="other">The result to compare against.</param>
    /// <returns>
    /// <c>true</c> when the result value members are equal; otherwise <c>false</c>.
    /// </returns>
    public virtual bool Equals(Result? other)
        => other is not null
        && IsSuccess == other.IsSuccess
        && StatusCode == other.StatusCode
        && Errors.SequenceEqual(other.Errors);

    /// <summary>
    /// Computes the hash code for this result.
    /// </summary>
    /// <returns>The combined hash code of the result value members.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(IsSuccess);
        hash.Add(StatusCode);

        foreach (var error in Errors)
        {
            hash.Add(error);
        }

        return hash.ToHashCode();
    }

    #endregion

    #region ToString

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns>
    /// <c>"Success (StatusCode)"</c> or <c>"Failure (StatusCode): Error1, Error2"</c>.
    /// </returns>
    public override string ToString()
        => IsSuccess
            ? $"Success ({StatusCode})"
            : $"Failure ({StatusCode}): {string.Join(", ", Errors)}";

    #endregion

    #region Helpers

    /// <summary>
    /// Resolves the default status code based on success state and errors.
    /// </summary>
    /// <param name="isSuccess">Whether the result is successful.</param>
    /// <param name="errors">The list of errors.</param>
    /// <returns>The default HTTP status code.</returns>
    protected static int ResolveDefaultStatusCode(
        bool isSuccess,
        List<Error> errors)
    {
        if (isSuccess)
        {
            return ResultConstant.StatusCode.Ok;
        }

        if (errors.Count == 0)
        {
            return ResultConstant.Default.FailureStatus;
        }

        var statusCode = errors[0].Status;

        return statusCode
            ?? ResultConstant.Default.FailureStatus;
    }

    #endregion
}
using System.Diagnostics.CodeAnalysis;

namespace Core.BuildingBlocks.Functional.Results;

#region Result

/// <summary>
/// Defines the contract for a result carrying success/failure state and errors.
/// </summary>
/// <typeparam name="TError">The error type.</typeparam>
/// <remarks>
/// Results model the outcome of operations that can fail without throwing exceptions.
/// They carry a success flag, optional errors, an HTTP status code, and metadata.
/// </remarks>
public interface IResult<TError> : IHasMetadata
    where TError : IError
{
    #region State

    /// <summary>
    /// Gets a value indicating whether the result has a value (is successful).
    /// </summary>
    bool HasValue { get; }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    bool IsFailure { get; }

    /// <summary>
    /// Gets the HTTP status code associated with the result.
    /// </summary>
    int StatusCode { get; }

    #endregion

    #region Errors

    /// <summary>
    /// Gets the list of errors when the result is a failure.
    /// </summary>
    List<TError> Errors { get; }

    #endregion
}

#endregion

#region Result<TValue>

/// <summary>
/// Defines the contract for a result carrying a value on success.
/// </summary>
/// <typeparam name="TValue">The value type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
/// <remarks>
/// On success, the result contains a value of type <typeparamref name="TValue"/>.
/// On failure, the value is inaccessible and the Errors list contains the failure details.
/// </remarks>
public interface IResult<TValue, TError> : IResult<TError>
    where TError : IError
{
    #region Value

    /// <summary>
    /// Gets the value when the result is successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The result is a failure.
    /// </exception>
    [AllowNull]
    TValue Value { get; }

    /// <summary>
    /// Attempts to get the value when the result is successful.
    /// </summary>
    /// <param name="value">The value if successful; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if the result is successful; otherwise <c>false</c>.</returns>
    bool TryGetValue(
        [MaybeNullWhen(false)] out TValue value);

    #endregion
}

#endregion
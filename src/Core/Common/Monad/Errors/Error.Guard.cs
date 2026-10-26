namespace Core.Common.Monad.Errors;

/// <summary>
/// Guard utilities for validating error member constraints.
/// </summary>
/// <remarks>
/// Centralizes validation logic so that <see cref="Error"/> constructors and
/// factory methods share consistent constraints and error messages.
/// </remarks>
public static class ErrorGuard
{
    /// <summary>
    /// Validates an error code.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="code"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="code"/> is whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="code"/> exceeds the maximum allowed length.
    /// </exception>
    public static void ValidateCode(string? code)
    {
        var value = Required(
            code,
            nameof(code),
            ErrorConstant.Result.Failure.Code.NullOrWhitespace.Code,
            ErrorConstant.Result.Failure.Code.NullOrWhitespace.Message);

        MaxLength(
            value,
            nameof(code),
            ErrorConstant.Constraint.Code.MaxLength,
            ErrorConstant.Result.Failure.Code.ExceedsMaxLength.Code,
            ErrorConstant.Result.Failure.Code.ExceedsMaxLength.Message);
    }

    /// <summary>
    /// Validates an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="message"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="message"/> is whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="message"/> exceeds the maximum allowed length.
    /// </exception>
    public static void ValidateMessage(string? message)
    {
        var value = Required(
            message,
            nameof(message),
            ErrorConstant.Result.Failure.Message.NullOrWhitespace.Code,
            ErrorConstant.Result.Failure.Message.NullOrWhitespace.Message);

        MaxLength(
            value,
            nameof(message),
            ErrorConstant.Constraint.Message.MaxLength,
            ErrorConstant.Result.Failure.Message.ExceedsMaxLength.Code,
            ErrorConstant.Result.Failure.Message.ExceedsMaxLength.Message);
    }

    /// <summary>
    /// Validates an error type.
    /// </summary>
    /// <param name="type">The error type.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="type"/> exceeds the maximum allowed length.
    /// </exception>
    public static void ValidateType(string? type)
    {
        if (type is null)
            return;

        MaxLength(
            type,
            nameof(type),
            ErrorConstant.Constraint.Type.MaxLength,
            ErrorConstant.Result.Failure.Type.ExceedsMaxLength.Code,
            ErrorConstant.Result.Failure.Type.ExceedsMaxLength.Message);
    }

    /// <summary>
    /// Validates an HTTP status code.
    /// </summary>
    /// <param name="status">The HTTP status code.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="status"/> is outside the valid HTTP range.
    /// </exception>
    public static void ValidateStatus(int status)
    {
        if (status is >= ErrorConstant.Constraint.Status.Min
            and <= ErrorConstant.Constraint.Status.Max)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(
            nameof(status),
            status,
            Format(
                ErrorConstant.Result.Failure.Status.OutOfRange.Code,
                ErrorConstant.Result.Failure.Status.OutOfRange.Message,
                ErrorConstant.Constraint.Status.Min,
                ErrorConstant.Constraint.Status.Max));
    }

    /// <summary>
    /// Validates an instance identifier.
    /// </summary>
    /// <param name="instance">The instance identifier.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="instance"/> exceeds the maximum allowed length.
    /// </exception>
    public static void ValidateInstance(string? instance)
    {
        if (instance is null)
            return;

        MaxLength(
            instance,
            nameof(instance),
            ErrorConstant.Constraint.Instance.MaxLength,
            ErrorConstant.Result.Failure.Instance.ExceedsMaxLength.Code,
            ErrorConstant.Result.Failure.Instance.ExceedsMaxLength.Message);
    }

    private static string Required(
        string? value,
        string parameterName,
        string errorCode,
        string errorMessage)
    {
        if (value is null)
        {
            throw new ArgumentNullException(
                parameterName,
                Format(errorCode, errorMessage));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                Format(errorCode, errorMessage),
                parameterName);
        }

        return value;
    }

    private static void MaxLength(
        string value,
        string parameterName,
        int maxLength,
        string errorCode,
        string errorMessage)
    {
        if (value.Length <= maxLength)
            return;

        throw new ArgumentOutOfRangeException(
            parameterName,
            value,
            Format(errorCode, errorMessage, maxLength));
    }

    private static string Format(
        string code,
        string message,
        params object[] args)
        => $"{code}: {string.Format(message, args)}";
}
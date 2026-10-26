namespace Core.Common.Monad.Results;

public static partial class ResultExtension
{
    #region Standard Metadata

    /// <summary>
    /// Attaches a trace identifier and returns a new result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="value">The trace identifier.</param>
    /// <returns>A new result with the trace ID metadata.</returns>
    public static Result WithTraceId(
        this Result result,
        object value)
        => result.WithMetadata(
            ErrorConstant.Metadata.TraceId,
            value);

    /// <summary>
    /// Attaches a trace identifier and returns a new result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="value">The trace identifier.</param>
    /// <returns>A new result with the trace ID metadata.</returns>
    public static Result<TValue> WithTraceId<TValue>(
        this Result<TValue> result,
        object value)
        => result.WithMetadata(
            ErrorConstant.Metadata.TraceId,
            value);

    /// <summary>
    /// Attaches a timestamp and returns a new result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="value">The timestamp.</param>
    /// <returns>A new result with the timestamp metadata.</returns>
    public static Result WithTimestamp(
        this Result result,
        object value)
        => result.WithMetadata(
            ErrorConstant.Metadata.Timestamp,
            value);

    /// <summary>
    /// Attaches a timestamp and returns a new result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="value">The timestamp.</param>
    /// <returns>A new result with the timestamp metadata.</returns>
    public static Result<TValue> WithTimestamp<TValue>(
        this Result<TValue> result,
        object value)
        => result.WithMetadata(
            ErrorConstant.Metadata.Timestamp,
            value);

    /// <summary>
    /// Attaches a resource identifier and returns a new result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="value">The resource identifier.</param>
    /// <returns>A new result with the resource metadata.</returns>
    public static Result WithResource(
        this Result result,
        object value)
        => result.WithMetadata(
            ErrorConstant.Metadata.Resource,
            value);

    /// <summary>
    /// Attaches a resource identifier and returns a new result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="value">The resource identifier.</param>
    /// <returns>A new result with the resource metadata.</returns>
    public static Result<TValue> WithResource<TValue>(
        this Result<TValue> result,
        object value)
        => result.WithMetadata(
            ErrorConstant.Metadata.Resource,
            value);

    /// <summary>
    /// Attaches a field identifier and returns a new result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="value">The field identifier.</param>
    /// <returns>A new result with the field metadata.</returns>
    public static Result WithField(
        this Result result,
        object value)
        => result.WithMetadata(
            ErrorConstant.Metadata.Field,
            value);

    /// <summary>
    /// Attaches a field identifier and returns a new result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="value">The field identifier.</param>
    /// <returns>A new result with the field metadata.</returns>
    public static Result<TValue> WithField<TValue>(
        this Result<TValue> result,
        object value)
        => result.WithMetadata(
            ErrorConstant.Metadata.Field,
            value);

    /// <summary>
    /// Attaches an attempt number and returns a new result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="value">The attempt number.</param>
    /// <returns>A new result with the attempt metadata.</returns>
    public static Result WithAttempt(
        this Result result,
        object value)
        => result.WithMetadata(
            ErrorConstant.Metadata.Attempt,
            value);

    /// <summary>
    /// Attaches an attempt number and returns a new result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="value">The attempt number.</param>
    /// <returns>A new result with the attempt metadata.</returns>
    public static Result<TValue> WithAttempt<TValue>(
        this Result<TValue> result,
        object value)
        => result.WithMetadata(
            ErrorConstant.Metadata.Attempt,
            value);

    #endregion
}

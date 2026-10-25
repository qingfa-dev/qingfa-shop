namespace Core.Common.Monad.Results;

/// <summary>
/// Extension methods for <see cref="Result"/> and <see cref="Result{TValue}"/>.
/// </summary>
/// <remarks>
/// Provides metadata manipulation, status code overrides, and internal
/// metadata propagation utilities.
/// </remarks>
public static partial class ResultExtension
{
    #region Metadata

    /// <summary>
    /// Adds or replaces a metadata entry and returns a new result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>A new result with the updated metadata.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static Result WithMetadata(
        this Result result,
        string key,
        object value)
    {
        ArgumentNullException.ThrowIfNull(result);

        var metadata = DefaultMetadata.Create(result.Metadata);

        metadata.SetValue(key, value);

        return result.CopyWith(
            metadata: metadata);
    }

    /// <summary>
    /// Adds or replaces a metadata entry and returns a new result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>A new result with the updated metadata.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static Result<TValue> WithMetadata<TValue>(
        this Result<TValue> result,
        string key,
        object value)
    {
        ArgumentNullException.ThrowIfNull(result);

        var metadata = DefaultMetadata.Create(result.Metadata);

        metadata.SetValue(key, value);

        return result.CopyWith(
            metadata: metadata);
    }

    /// <summary>
    /// Replaces the metadata dictionary and returns a new result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="metadata">The replacement metadata.</param>
    /// <returns>A new result with the updated metadata.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> or <paramref name="metadata"/> is <c>null</c>.
    /// </exception>
    public static Result WithMetadata(
        this Result result,
        DefaultMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(metadata);

        return result.CopyWith(
            metadata: DefaultMetadata.Create(metadata));
    }

    /// <summary>
    /// Replaces the metadata dictionary and returns a new result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="metadata">The replacement metadata.</param>
    /// <returns>A new result with the updated metadata.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> or <paramref name="metadata"/> is <c>null</c>.
    /// </exception>
    public static Result<TValue> WithMetadata<TValue>(
        this Result<TValue> result,
        DefaultMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(metadata);

        return result.CopyWith(
            metadata: DefaultMetadata.Create(metadata));
    }

    #endregion

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

    #region Status

    /// <summary>
    /// Replaces the HTTP status code and returns a new result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="statusCode">The new HTTP status code.</param>
    /// <returns>A new result with the updated status code.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static Result WithStatus(
        this Result result,
        int statusCode)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.CopyWith(
            statusCode: statusCode);
    }

    /// <summary>
    /// Replaces the HTTP status code and returns a new result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="statusCode">The new HTTP status code.</param>
    /// <returns>A new result with the updated status code.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static Result<TValue> WithStatus<TValue>(
        this Result<TValue> result,
        int statusCode)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.CopyWith(
            statusCode: statusCode);
    }

    #endregion

    #region Internal Propagation

    /// <summary>
    /// Propagates metadata from a source result to a target result.
    /// </summary>
    /// <param name="result">The target result.</param>
    /// <param name="source">The source result.</param>
    /// <returns>A new result with metadata copied from the source.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> or <paramref name="source"/> is <c>null</c>.
    /// </exception>
    internal static Result WithMetadataFrom(
        this Result result,
        Result source)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(source);

        return result.CopyWith(
            metadata: DefaultMetadata.Create(
                source.Metadata));
    }

    /// <summary>
    /// Propagates metadata from a source result to a target result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="result">The target result.</param>
    /// <param name="source">The source result.</param>
    /// <returns>A new result with metadata copied from the source.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> or <paramref name="source"/> is <c>null</c>.
    /// </exception>
    internal static Result<TValue> WithMetadataFrom<TValue>(
        this Result<TValue> result,
        Result source)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(source);

        return result.CopyWith(
            metadata: DefaultMetadata.Create(
                source.Metadata));
    }

    #endregion
}
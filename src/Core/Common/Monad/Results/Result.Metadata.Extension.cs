namespace Core.Common.Monad.Results;

/// <summary>
/// Extension methods for <see cref="Result"/> and <see cref="Result{TValue}"/>.
/// </summary>
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
}

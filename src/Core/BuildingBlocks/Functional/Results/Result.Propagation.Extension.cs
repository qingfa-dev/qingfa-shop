namespace Core.BuildingBlocks.Functional.Results;

public static partial class ResultExtension
{
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

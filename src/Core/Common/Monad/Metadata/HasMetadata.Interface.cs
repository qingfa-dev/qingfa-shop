namespace Core.Common.Monad.Metadata;

/// <summary>
/// Defines a contract for objects that carry metadata.
/// </summary>
/// <typeparam name="TMetadata">The metadata type.</typeparam>
/// <remarks>
/// Implementations expose a metadata dictionary for attaching
/// contextual information such as trace IDs, timestamps, and resource identifiers.
/// </remarks>
public interface IHasMetadata<TMetadata>
    where TMetadata : IMetadata
{
    /// <summary>
    /// Gets the metadata dictionary.
    /// </summary>
    TMetadata Metadata { get; }
}

/// <summary>
/// Non-generic variant using the default <see cref="DefaultMetadata"/>.
/// </summary>
public interface IHasMetadata : IHasMetadata<DefaultMetadata>
{
}
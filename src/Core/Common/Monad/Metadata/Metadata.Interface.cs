namespace Core.Common.Monad.Metadata;

/// <summary>
/// Defines a read-only dictionary of metadata key-value pairs.
/// </summary>
/// <remarks>
/// Metadata provides a flexible, extensible mechanism for attaching
/// arbitrary contextual information to errors, results, and domain objects.
/// Keys are case-insensitive.
/// </remarks>
public interface IMetadata : IReadOnlyDictionary<string, object>
{
}
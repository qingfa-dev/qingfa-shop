namespace Core.Common.Concerns.Content.Metafieldable;

/// <summary>Represents an entity that supports metafields.</summary>
public interface IMetafieldable
{
    /// <summary>Gets the collection of metafields.</summary>
    ICollection<Metafield> Metafields { get; }
}
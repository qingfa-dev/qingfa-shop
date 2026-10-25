namespace Core.Common.Concerns.Content.Taggable;

/// <summary>Represents an entity that supports tags.</summary>
public interface ITaggable
{
    /// <summary>Gets the collection of tags.</summary>
    ICollection<string> Tags { get; }
}
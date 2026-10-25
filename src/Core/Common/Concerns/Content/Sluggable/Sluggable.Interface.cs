namespace Core.Common.Concerns.Content.Sluggable;

/// <summary>Represents an entity that supports slugs.</summary>
public interface ISluggable
{
    /// <summary>Gets or sets the slug.</summary>
    string Slug { get; set; }
}
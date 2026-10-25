namespace Core.Common.Concerns.Lifecycle.Publishable;

/// <summary>Base concern for publication state tracking.</summary>
public interface IPublishable
{
    /// <summary>Gets or sets a value indicating whether the entity is published.</summary>
    bool IsPublished { get; set; }
    /// <summary>Gets or sets the UTC timestamp when the entity was published.</summary>
    DateTimeOffset? PublishedAtUtc { get; set; }
    /// <summary>Gets or sets the actor who published the entity.</summary>
    string? PublishedBy { get; set; }
}
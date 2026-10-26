namespace Core.Common.Concerns.Lifecycle.SoftDeletable;

/// <summary>Base concern for soft deletion state tracking.</summary>
public interface ISoftDeletable
{
    /// <summary>Gets or sets a value indicating whether the entity is deleted.</summary>
    bool IsDeleted { get; set; }
    /// <summary>Gets or sets the UTC timestamp when the entity was deleted.</summary>
    DateTimeOffset? DeletedAtUtc { get; set; }
    /// <summary>Gets or sets the actor who deleted the entity.</summary>
    string? DeletedBy { get; set; }
}
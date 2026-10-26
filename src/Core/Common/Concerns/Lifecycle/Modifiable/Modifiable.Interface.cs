namespace Core.Common.Concerns.Lifecycle.Modifiable;

/// <summary>Base concern for modification audit tracking.</summary>
public interface IModifiable
{
    /// <summary>Gets or sets the UTC timestamp when the entity was last modified.</summary>
    DateTimeOffset? ModifiedAtUtc { get; set; }
    /// <summary>Gets or sets the actor who last modified the entity.</summary>
    string? ModifiedBy { get; set; }
}
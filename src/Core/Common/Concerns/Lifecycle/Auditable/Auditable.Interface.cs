namespace Core.Common.Concerns.Lifecycle.Auditable;

/// <summary>Audit metadata carried by entities whose creation and modification must be attributable.</summary>
public interface IAuditable
{
    /// <summary>Gets or sets the UTC timestamp when the entity was created.</summary>
    DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the actor that created the entity.</summary>
    string? CreatedBy { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the entity was last modified.</summary>
    DateTimeOffset? ModifiedAtUtc { get; set; }

    /// <summary>Gets or sets the actor that last modified the entity.</summary>
    string? ModifiedBy { get; set; }
}

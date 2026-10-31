namespace Core.BuildingBlocks.Modeling.Concerns.Content.Localizable;

/// <summary>Represents an entity that supports localized values.</summary>
public interface ILocalizable
{
    /// <summary>Gets the collection of localized values.</summary>
    ICollection<LocalizedValue> LocalizedValues { get; }
}
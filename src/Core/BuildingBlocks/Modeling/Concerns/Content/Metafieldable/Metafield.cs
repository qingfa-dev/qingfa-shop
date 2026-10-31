namespace Core.BuildingBlocks.Modeling.Concerns.Content.Metafieldable;

/// <summary>Represents a metafield with namespace, key, type, and value.</summary>
public sealed class Metafield
{
    #region Properties

    /// <summary>Gets or sets the namespace.</summary>
    public string Namespace { get; set; } = null!;
    /// <summary>Gets or sets the key.</summary>
    public string Key { get; set; } = null!;
    /// <summary>Gets or sets the metafield type.</summary>
    public MetafieldType Type { get; set; }
    /// <summary>Gets or sets the value.</summary>
    public string? Value { get; set; }

    #endregion
}
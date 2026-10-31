namespace Core.BuildingBlocks.Modeling.Concerns.Content.Metafieldable;

/// <summary>The type of a metafield value.</summary>
public enum MetafieldType
{
    /// <summary>Short text value.</summary>
    ShortText,
    /// <summary>Long text value.</summary>
    LongText,
    /// <summary>Rich text value.</summary>
    RichText,
    /// <summary>Numeric value.</summary>
    Number,
    /// <summary>Boolean value.</summary>
    Boolean,
    /// <summary>JSON value.</summary>
    Json
}
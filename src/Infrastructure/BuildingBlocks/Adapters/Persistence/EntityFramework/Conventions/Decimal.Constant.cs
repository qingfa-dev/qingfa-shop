namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;

/// <summary>Decimal precision defaults shared by the convention and the value converters.</summary>
public static class DecimalConstant
{
    /// <summary>Default precision for decimal columns.</summary>
    public const int DefaultPrecision = 18;

    /// <summary>Default scale for decimal columns.</summary>
    public const int DefaultScale = 6;
}

namespace Core.BuildingBlocks.Domain.ValueObjects.Percentage;

/// <summary>Defines the inclusive percentage limits.</summary>
public static class PercentageConstant
{
    /// <summary>Percentage validation limits.</summary>
    public static class Constraints
    {
        /// <summary>Percentage value limits.</summary>
        public static class Value
        {
            /// <summary>The smallest accepted percentage.</summary>
            public const decimal MinValue = 0m;
            /// <summary>The largest accepted percentage.</summary>
            public const decimal MaxValue = 100m;
        }
    }

    /// <summary>Default percentage values.</summary>
    public static class Defaults
    {
        /// <summary>Zero percent.</summary>
        public const decimal Zero = 0m;
        /// <summary>One hundred percent.</summary>
        public const decimal Hundred = 100m;
    }
}
namespace Core.BuildingBlocks.Domain.ValueObjects.Measurement;

/// <summary>Constants for <see cref="Dimensions"/> validation.</summary>
public static class DimensionsConstant
{
    /// <summary>Constraint values for dimensions validation.</summary>
    public static class Constraints
    {
        /// <summary>Length measurement constraints.</summary>
        public static class Length
        {
            /// <summary>The smallest accepted length measurement.</summary>
            public const decimal MinValue = 0m;
        }

        /// <summary>Width measurement constraints.</summary>
        public static class Width
        {
            /// <summary>The smallest accepted width measurement.</summary>
            public const decimal MinValue = 0m;
        }

        /// <summary>Height measurement constraints.</summary>
        public static class Height
        {
            /// <summary>The smallest accepted height measurement.</summary>
            public const decimal MinValue = 0m;
        }
    }
}
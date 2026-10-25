namespace Core.Common.Domain.ValueObjects.Measurement;

/// <summary>Constants for <see cref="Weight"/> validation.</summary>
public static class WeightConstant
{
    /// <summary>Constraint values for weight validation.</summary>
    public static class Constraints
    {
        /// <summary>Weight amount constraints.</summary>
        public static class Value
        {
            /// <summary>The smallest accepted weight amount.</summary>
            public const decimal MinValue = 0m;
        }
    }

    /// <summary>Default values for weight creation.</summary>
    public static class Defaults
    {
        /// <summary>The default weight amount.</summary>
        public const decimal Value = 0m;
    }
}
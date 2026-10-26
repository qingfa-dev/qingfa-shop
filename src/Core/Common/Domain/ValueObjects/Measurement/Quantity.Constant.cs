namespace Core.Common.Domain.ValueObjects.Measurement;

/// <summary>Constants for <see cref="Quantity"/> validation.</summary>
public static class QuantityConstant
{
    /// <summary>Constraint values for quantity validation.</summary>
    public static class Constraints
    {
        /// <summary>Quantity amount constraints.</summary>
        public static class Value
        {
            /// <summary>The smallest accepted quantity amount.</summary>
            public const decimal MinValue = 0m;
        }
    }

    /// <summary>Default values for quantity creation.</summary>
    public static class Defaults
    {
        /// <summary>The default quantity amount.</summary>
        public const decimal Value = 0m;
    }
}
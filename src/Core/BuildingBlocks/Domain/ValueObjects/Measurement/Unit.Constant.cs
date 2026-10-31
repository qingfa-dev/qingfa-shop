namespace Core.BuildingBlocks.Domain.ValueObjects.Measurement;

/// <summary>Constants for <see cref="Unit"/> validation.</summary>
public static class UnitConstant
{
    /// <summary>Constraint values for unit-code validation.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for a unit code.</summary>
        public static class Code
        {
            /// <summary>Minimum length for a unit code.</summary>
            public const int MinLength = 1;

            /// <summary>Maximum length for a unit code.</summary>
            public const int MaxLength = 4;
        }
    }

    /// <summary>Regex patterns for unit-code validation.</summary>
    public static class Patterns
    {
        /// <summary>Accepts one to four lowercase ASCII letters.</summary>
        public const string Code = @"^[a-z]{1,4}$";
    }
}
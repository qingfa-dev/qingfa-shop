namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.Versionable;

/// <summary>Constants for the Versionable concern.</summary>
public static class VersionableConstant
{
    /// <summary>Constraint values for version validation.</summary>
    public static class Constraints
    {
        /// <summary>Minimum allowed version value.</summary>
        public const long MinVersion = Version.Min;

        /// <summary>Maximum allowed version value.</summary>
        public const long MaxVersion = Version.Max;

        /// <summary>Version range constraints.</summary>
        public static class Version
        {
            /// <summary>Minimum allowed version value.</summary>
            public const long Min = 0;

            /// <summary>Maximum allowed version value.</summary>
            public const long Max = long.MaxValue;
        }
    }

    /// <summary>Default values for the Versionable concern.</summary>
    public static class Defaults
    {
        /// <summary>Initial version assigned to new entities.</summary>
        public const long InitialVersion = 0;

        /// <summary>Amount added to the version by a single bump.</summary>
        public const long Increment = 1;
    }
}
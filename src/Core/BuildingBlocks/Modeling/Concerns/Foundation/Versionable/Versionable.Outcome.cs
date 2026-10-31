namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.Versionable;

/// <summary>Defines failure errors for the Versionable concern.</summary>
public static class VersionableOutcome
{
    /// <summary>Validation error codes for version validation.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the entity itself is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Versionable entity is required.");

        /// <summary>Error when the version is negative.</summary>
        public static Error VersionNegative => Error.UnprocessableEntity(
            code: Codes.VersionNegative,
            message:
                $"Version must not be negative (minimum is {VersionableConstant.Constraints.Version.Min}).");

        /// <summary>Error when the version is already at its maximum value.</summary>
        public static Error VersionOverflow => Error.UnprocessableEntity(
            code: Codes.VersionOverflow,
            message:
                $"Version is already at its maximum value of {VersionableConstant.Constraints.Version.Max}.");

        /// <summary>Error when the version falls outside the allowed range.</summary>
        public static Error VersionOutOfRange => Error.UnprocessableEntity(
            code: Codes.VersionOutOfRange,
            message:
                $"Version must be between {VersionableConstant.Constraints.Version.Min} and {VersionableConstant.Constraints.Version.Max}.");

        /// <summary>Error when the entity version does not match the expected version.</summary>
        /// <param name="expected">The version the caller expects.</param>
        /// <param name="actual">The version the entity currently carries.</param>
        /// <returns>A conflict error describing the mismatch.</returns>
        public static Error ConcurrencyConflict(
            long expected,
            long actual) => Error.Conflict(
            code: Codes.ConcurrencyConflict,
            message: $"Version conflict: expected '{expected}' but found '{actual}'.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Versionable.Entity.Required";
        public const string VersionNegative = "Versionable.Version.Negative";
        public const string VersionOverflow = "Versionable.Version.Overflow";
        public const string VersionOutOfRange = "Versionable.Version.OutOfRange";
        public const string ConcurrencyConflict = "Versionable.Version.ConcurrencyConflict";
    }
}
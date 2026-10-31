namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Positionable;

/// <summary>Result constants for <see cref="IPositionable"/>.</summary>
public static class PositionableOutcome
{
    /// <summary>Failure error codes for positionable operations.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but null.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Positionable entity is required.");

        /// <summary>Error for when the position is negative.</summary>
        public static Error PositionNegative => Error.UnprocessableEntity(
            code: Codes.PositionNegative,
            message:
                $"Position must not be less than {PositionableConstant.Constraints.Position.Min}.");

        /// <summary>Error for when the position exceeds the maximum.</summary>
        public static Error PositionTooLarge => Error.UnprocessableEntity(
            code: Codes.PositionTooLarge,
            message:
                $"Position must not exceed {PositionableConstant.Constraints.Position.Max}.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Positionable.Entity.Required";
        public const string PositionNegative = "Positionable.Position.Negative";
        public const string PositionTooLarge = "Positionable.Position.TooLarge";
    }
}
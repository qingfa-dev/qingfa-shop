namespace Core.Common.Concerns.Organization.Positionable;

/// <summary>Result constants for <see cref="IPositionable"/>.</summary>
public static class PositionableResult
{
    /// <summary>Failure error codes for positionable operations.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but null.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Positionable.Entity.Required",
            message: "Positionable entity is required.");

        /// <summary>Error for when the position is negative.</summary>
        public static Error PositionNegative => Error.UnprocessableEntity(
            code: "Positionable.Position.Negative",
            message:
                $"Position must not be less than {PositionableConstant.Constraints.Position.Min}.");

        /// <summary>Error for when the position exceeds the maximum.</summary>
        public static Error PositionTooLarge => Error.UnprocessableEntity(
            code: "Positionable.Position.TooLarge",
            message:
                $"Position must not exceed {PositionableConstant.Constraints.Position.Max}.");

        #endregion
    }
}
namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Positionable;

/// <summary>Constants for <see cref="IPositionable"/> validation.</summary>
public static class PositionableConstant
{
    #region Constraints

    /// <summary>Constraint values for positionable validation.</summary>
    public static class Constraints
    {
        /// <summary>Position value constraints.</summary>
        public static class Position
        {
            /// <summary>The minimum allowed position value.</summary>
            public const int Min = 0;

            /// <summary>The maximum allowed position value.</summary>
            public const int Max = int.MaxValue;

            /// <summary>The step used by increment/decrement helpers.</summary>
            public const int Step = 1;
        }

        /// <summary>The minimum allowed position value.</summary>
        /// <remarks>Obsolete shim kept for backward compatibility. Use <see cref="Position.Min"/> instead.</remarks>
        [Obsolete("Use Constraints.Position.Min instead.")]
        public const int MinPosition = Position.Min;
    }

    #endregion

    #region Defaults

    /// <summary>Default option values for the <see cref="Positionable"/> concern.</summary>
    public static class Defaults
    {
        /// <summary>The default position for new entities.</summary>
        public const int DefaultPosition = 0;
    }

    #endregion
}
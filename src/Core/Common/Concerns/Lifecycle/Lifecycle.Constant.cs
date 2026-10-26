namespace Core.Common.Concerns.Lifecycle;

/// <summary>Constants for the <see cref="Lifecycle"/> concern.</summary>
public static class LifecycleConstant
{
    #region Constraints

    /// <summary>Constraint values for the <see cref="Lifecycle"/> concern.</summary>
    public static class Constraints
    {
        /// <summary>Actor name constraints shared by all lifecycle concerns.</summary>
        public static class Actor
        {
            /// <summary>Minimum length for an actor name.</summary>
            public const int MinLength = 2;

            /// <summary>Maximum length for an actor name.</summary>
            public const int MaxLength = 256;
        }

        /// <summary>Timestamp constraints shared by all lifecycle concerns.</summary>
        public static class Timestamp
        {
            /// <summary>Maximum allowed clock skew for timestamps in the future, in seconds.</summary>
            public const int MaxFutureSkewSeconds = 300;
        }

        /// <summary>Maximum length for an actor name.</summary>
        /// <remarks>Backward-compatibility shim; prefer <see cref="Actor.MaxLength"/> in new code.</remarks>
        public const int MaxActorLength = Actor.MaxLength;
    }

    #endregion

    #region Defaults

    /// <summary>Default option values for the <see cref="Lifecycle"/> concern.</summary>
    public static class Defaults
    {
        /// <summary>Default value for requiring an actor on lifecycle operations.</summary>
        public const bool RequireActor = false;
    }

    #endregion

    #region Patterns

    /// <summary>Regex patterns for the <see cref="Lifecycle"/> concern.</summary>
    public static class Patterns
    {
        /// <summary>Pattern for actor name validation (no leading/trailing whitespace).</summary>
        public const string Actor = @"^\S(?:.*\S)?$";
    }

    #endregion
}
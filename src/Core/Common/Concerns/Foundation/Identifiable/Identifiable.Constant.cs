namespace Core.Common.Concerns.Foundation.Identifiable;

/// <summary>Constants for the Identifiable concern.</summary>
public static class IdentifiableConstant
{
    /// <summary>Constraint values for identifier validation.</summary>
    public static class Constraints
    {
        /// <summary>Identifier constraints.</summary>
        public static class Id
        {
            /// <summary>Whether the default value for the key type is allowed. Always false.</summary>
            public const bool AllowDefault = false;
        }
    }

    /// <summary>Default values for the Identifiable concern.</summary>
    public static class Defaults
    {
        /// <summary>Empty identifier sentinel for <see cref="Guid"/> keys.</summary>
        public static readonly Guid EmptyId = Guid.Empty;
    }
}
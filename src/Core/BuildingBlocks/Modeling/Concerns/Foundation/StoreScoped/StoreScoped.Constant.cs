namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.StoreScoped;

/// <summary>Constants for the StoreScoped concern.</summary>
public static class StoreScopedConstant
{
    /// <summary>Constraint values for store-scope validation.</summary>
    public static class Constraints
    {
        /// <summary>Store identifier constraints.</summary>
        public static class Store
        {
            /// <summary>Whether the default value for the store key type is allowed. Always false.</summary>
            public const bool AllowDefault = false;
        }
    }

    /// <summary>Default values for the StoreScoped concern.</summary>
    public static class Defaults
    {
        /// <summary>Empty store identifier sentinel for <see cref="Guid"/> keys.</summary>
        public static readonly Guid EmptyStoreId = Guid.Empty;
    }
}
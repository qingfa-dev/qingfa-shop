namespace Core.Common.Concerns.Organization.Hierarchical;

/// <summary>Result constants for <see cref="IHierarchical{TKey}"/>.</summary>
public static class HierarchicalResult
{
    /// <summary>Failure error codes for hierarchical operations.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but null.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Hierarchical.Entity.Required",
            message: "Hierarchical entity is required.");

        /// <summary>Error for when the parent identifier is empty.</summary>
        public static Error ParentEmpty => Error.UnprocessableEntity(
            code: "Hierarchical.Parent.Empty",
            message: "Parent identifier must not be empty. Use null for root entities.");

        /// <summary>Error for when the entity is assigned as its own parent.</summary>
        public static Error SelfParent => Error.UnprocessableEntity(
            code: "Hierarchical.Parent.SelfParent",
            message: "Entity cannot be its own parent.");

        /// <summary>Error for when the parent assignment would create a cycle.</summary>
        public static Error CycleDetected => Error.UnprocessableEntity(
            code: "Hierarchical.Parent.CycleDetected",
            message: "Parent assignment would create a cycle.");

        /// <summary>Error for when the hierarchy depth exceeds the maximum.</summary>
        public static Error DepthExceeded => Error.UnprocessableEntity(
            code: "Hierarchical.Depth.Exceeded",
            message:
                $"Hierarchy depth cannot exceed {HierarchicalConstant.Constraints.Parent.MaxDepth} levels.");

        /// <summary>Error for when the level is outside the allowed range.</summary>
        public static Error LevelOutOfRange => Error.UnprocessableEntity(
            code: "Hierarchical.Level.OutOfRange",
            message:
                $"Level must be between {HierarchicalConstant.Constraints.Level.Min} and {HierarchicalConstant.Constraints.Level.Max}.");

        /// <summary>Error for when the materialized path exceeds the maximum length.</summary>
        public static Error PathTooLong => Error.UnprocessableEntity(
            code: "Hierarchical.Path.TooLong",
            message:
                $"Path cannot exceed {HierarchicalConstant.Constraints.Parent.MaxPathLength} characters.");

        #endregion
    }
}
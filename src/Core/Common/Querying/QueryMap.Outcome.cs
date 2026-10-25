namespace Core.Common.Querying;

/// <summary>Result constants for the query map.</summary>
/// <remarks>
/// A map is declared once, during composition. These failures describe a broken
/// declaration, so the caller that builds the map reports them and refuses to
/// start rather than letting a half-built map reach the registry.
/// </remarks>
public static class QueryMapOutcome
{
    /// <summary>Failure error constants for the query map.</summary>
    public static class Failure
    {
        #region Declaration

        /// <summary>Error when a mapped field has no name.</summary>
        public static Error NameRequired => Error.BadRequest(
            code: "Query.Map.NameRequired",
            message: "A mapped field needs a name.");

        /// <summary>Error when a mapped field's name exceeds the length limit.</summary>
        public static Error NameTooLong(int max) => Error.BadRequest(
            code: "Query.Map.NameTooLong",
            message: $"A mapped field name cannot exceed {max} characters.");

        /// <summary>Error when a field name is already mapped.</summary>
        public static Error Duplicate(string name) => Error.BadRequest(
            code: "Query.Map.Duplicate",
            message: $"Field '{name}' is already mapped.");

        /// <summary>Error when a searchable field is not a scalar string.</summary>
        public static Error SearchableNotString(string name) => Error.BadRequest(
            code: "Query.Map.SearchableNotString",
            message: $"Field '{name}' is searchable but is not a scalar string.");

        /// <summary>Error when a composite map declares no members.</summary>
        public static Error CompositeEmpty => Error.BadRequest(
            code: "Query.Map.CompositeEmpty",
            message: "A composite map needs at least one member.");

        /// <summary>Error when a composite map's members disagree on their value type.</summary>
        public static Error CompositeTypeMismatch => Error.BadRequest(
            code: "Query.Map.CompositeTypeMismatch",
            message: "Composite members must share one value type.");

        #endregion
    }
}
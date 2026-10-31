namespace Application.BuildingBlocks.Querying.Mapping;
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
            code: QueryErrorCodes.MapNameRequired,
            message: "A mapped field needs a name.");

        /// <summary>Error when a mapped field's name exceeds the length limit.</summary>
        public static Error NameTooLong(int max) => Error.BadRequest(
            code: QueryErrorCodes.MapNameTooLong,
            message: $"A mapped field name cannot exceed {max} characters.");

        /// <summary>Error when a field name is already mapped.</summary>
        public static Error Duplicate(string name) => Error.BadRequest(
            code: QueryErrorCodes.MapDuplicate,
            message: $"Field '{name}' is already mapped.");

        /// <summary>Error when a searchable field is not a scalar string.</summary>
        public static Error SearchableNotString(string name) => Error.BadRequest(
            code: QueryErrorCodes.MapSearchableNotString,
            message: $"Field '{name}' is searchable but is not a scalar string.");

        /// <summary>Error when a composite map declares no members.</summary>
        public static Error CompositeEmpty => Error.BadRequest(
            code: QueryErrorCodes.MapCompositeEmpty,
            message: "A composite map needs at least one member.");

        /// <summary>Error when a composite map's members disagree on their value type.</summary>
        public static Error CompositeTypeMismatch => Error.BadRequest(
            code: QueryErrorCodes.MapCompositeTypeMismatch,
            message: "Composite members must share one value type.");

        #endregion

        #region Registry

        /// <summary>Error when a type already has a registered map.</summary>
        public static Error AlreadyRegistered(Type type) => Error.Conflict(
            code: QueryErrorCodes.MapAlreadyRegistered,
            message: $"A query map for '{type.Name}' is already registered.");

        /// <summary>Error when a type has no registered map.</summary>
        public static Error NotRegistered(Type type) => Error.InternalServerError(
            code: QueryErrorCodes.MapNotRegistered,
            message: $"No query map is registered for '{type.Name}'.");

        #endregion
    }
}
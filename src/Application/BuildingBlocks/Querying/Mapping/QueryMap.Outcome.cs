namespace Application.BuildingBlocks.Querying.Mapping;
/// <summary>Result constants for the query map.</summary>
/// <remarks>
/// A map is declared once, during composition. These failures describe a broken
/// declaration, so the caller that builds the map reports them and refuses to
/// start rather than letting a half-built map reach the registry.
/// </remarks>
public static class QueryMapOutcome
{
    /// <summary>Query code aliases owned by the shared query outcome.</summary>
    public static class Codes
    {
        /// <summary>See <see cref="QueryOutcome.Codes.MapNameRequired"/>.</summary>
        public const string MapNameRequired = QueryOutcome.Codes.MapNameRequired;

        /// <summary>See <see cref="QueryOutcome.Codes.MapNameTooLong"/>.</summary>
        public const string MapNameTooLong = QueryOutcome.Codes.MapNameTooLong;

        /// <summary>See <see cref="QueryOutcome.Codes.MapDuplicate"/>.</summary>
        public const string MapDuplicate = QueryOutcome.Codes.MapDuplicate;

        /// <summary>See <see cref="QueryOutcome.Codes.MapSearchableNotString"/>.</summary>
        public const string MapSearchableNotString = QueryOutcome.Codes.MapSearchableNotString;

        /// <summary>See <see cref="QueryOutcome.Codes.MapCompositeEmpty"/>.</summary>
        public const string MapCompositeEmpty = QueryOutcome.Codes.MapCompositeEmpty;

        /// <summary>See <see cref="QueryOutcome.Codes.MapCompositeTypeMismatch"/>.</summary>
        public const string MapCompositeTypeMismatch = QueryOutcome.Codes.MapCompositeTypeMismatch;

        /// <summary>See <see cref="QueryOutcome.Codes.MapAlreadyRegistered"/>.</summary>
        public const string MapAlreadyRegistered = QueryOutcome.Codes.MapAlreadyRegistered;

        /// <summary>See <see cref="QueryOutcome.Codes.MapNotRegistered"/>.</summary>
        public const string MapNotRegistered = QueryOutcome.Codes.MapNotRegistered;

    }

    /// <summary>Failure error constants for the query map.</summary>
    public static class Failure
    {
        #region Declaration

        /// <summary>Error when a mapped field has no name.</summary>
        public static Error NameRequired => Error.BadRequest(
            code: Codes.MapNameRequired,
            message: "A mapped field needs a name.");

        /// <summary>Error when a mapped field's name exceeds the length limit.</summary>
        public static Error NameTooLong(int max) => Error.BadRequest(
            code: Codes.MapNameTooLong,
            message: $"A mapped field name cannot exceed {max} characters.");

        /// <summary>Error when a field name is already mapped.</summary>
        public static Error Duplicate(string name) => Error.BadRequest(
            code: Codes.MapDuplicate,
            message: $"Field '{name}' is already mapped.");

        /// <summary>Error when a searchable field is not a scalar string.</summary>
        public static Error SearchableNotString(string name) => Error.BadRequest(
            code: Codes.MapSearchableNotString,
            message: $"Field '{name}' is searchable but is not a scalar string.");

        /// <summary>Error when a composite map declares no members.</summary>
        public static Error CompositeEmpty => Error.BadRequest(
            code: Codes.MapCompositeEmpty,
            message: "A composite map needs at least one member.");

        /// <summary>Error when a composite map's members disagree on their value type.</summary>
        public static Error CompositeTypeMismatch => Error.BadRequest(
            code: Codes.MapCompositeTypeMismatch,
            message: "Composite members must share one value type.");

        #endregion

        #region Registry

        /// <summary>Error when a type already has a registered map.</summary>
        public static Error AlreadyRegistered(Type type) => Error.Conflict(
            code: Codes.MapAlreadyRegistered,
            message: $"A query map for '{type.Name}' is already registered.");

        /// <summary>Error when a type has no registered map.</summary>
        public static Error NotRegistered(Type type) => Error.InternalServerError(
            code: Codes.MapNotRegistered,
            message: $"No query map is registered for '{type.Name}'.");

        #endregion
    }
}
namespace Application.Modules.Todos;

/// <summary>Application-layer failures specific to the Todos feature.</summary>
public static class TodosOutcome
{
    /// <summary>Failure errors shared by the Todos use cases.</summary>
    public static class Failure
    {
        /// <summary>Error returned when an advanced query asks for a tabular shape.</summary>
        public static Error TabularQueryUnsupported => Error.BadRequest(
            code: Codes.TabularQueryUnsupported,
            message: "This use case returns entities, so select, group, and aggregate are not supported.");

        /// <summary>Error returned when a tabular query omits its projection.</summary>
        public static Error EntityQueryUnsupported => Error.BadRequest(
            code: Codes.EntityQueryUnsupported,
            message: "This use case returns tabular data, so select, group, or aggregate is required.");

        /// <summary>Error returned when a sort field is not on the allow list.</summary>
        public static Error SortNotSupported(string field) => Error.BadRequest(
            code: Codes.SortNotSupported,
            message: $"The sort field '{field}' is not supported.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string TabularQueryUnsupported = "Todos.Query.TabularUnsupported";
        public const string EntityQueryUnsupported = "Todos.Query.EntityUnsupported";
        public const string SortNotSupported = "Todos.Sort.NotSupported";
    }
}

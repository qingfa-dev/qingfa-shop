namespace Application.Features.Todos;

/// <summary>Application-layer failures specific to the Todos feature.</summary>
public static class TodosOutcome
{
    /// <summary>Failure errors shared by the Todos use cases.</summary>
    public static class Failure
    {
        /// <summary>Error returned when an advanced query asks for a tabular shape.</summary>
        public static Error TabularQueryUnsupported => Error.BadRequest(
            code: "Todos.Query.TabularUnsupported",
            message: "This use case returns entities, so select, group, and aggregate are not supported.");

        /// <summary>Error returned when a sort field is not on the allow list.</summary>
        public static Error SortNotSupported(string field) => Error.BadRequest(
            code: "Todos.Sort.NotSupported",
            message: $"The sort field '{field}' is not supported.");
    }
}

namespace Application.Common.Querying;

/// <summary>Factory methods that create typed errors for query parsing and validation failures.</summary>
public static class QueryOutcome
{
    /// <summary>Factory methods for every query error scenario.</summary>
    public static class Failure
    {
        // ---- Filter ----

        /// <summary>The filter text contains a syntax error (unmatched parenthesis, unexpected token, etc.).</summary>
        public static Error Syntax(string detail) => Error.BadRequest(
            code: QueryErrorCodes.FilterSyntax,
            message: $"Invalid filter syntax: {detail}.");

        /// <summary>The filter value cannot be converted to the field's expected type.</summary>
        public static Error ValueInvalid(string field, string value) => Error.BadRequest(
            code: QueryErrorCodes.FilterValueInvalid,
            message: $"Value '{value}' is not valid for field '{field}'.");

        /// <summary>The filter text exceeds the maximum allowed length.</summary>
        public static Error TooLong(int max) => Error.BadRequest(
            code: QueryErrorCodes.FilterTooLong,
            message: $"Filter cannot exceed {max} characters.");

        /// <summary>The filter contains more nested parenthesis groups than allowed.</summary>
        public static Error DepthExceeded(int max) => Error.BadRequest(
            code: QueryErrorCodes.FilterDepthExceeded,
            message: $"Filter nesting cannot exceed {max} levels.");

        /// <summary>The filter contains more individual conditions than the limit allows.</summary>
        public static Error TooManyConditions(int max) => Error.BadRequest(
            code: QueryErrorCodes.FilterTooManyConditions,
            message: $"Filter cannot contain more than {max} conditions.");

        // ---- Field ----

        /// <summary>The referenced field name does not exist in the query map.</summary>
        public static Error NotMapped(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldNotMapped,
            message: $"Field '{field}' is not mapped.");

        /// <summary>The field exists but is marked as not filterable in the map.</summary>
        public static Error NotFilterable(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldNotFilterable,
            message: $"Field '{field}' cannot be filtered.");

        /// <summary>The field exists but is marked as not sortable in the map.</summary>
        public static Error NotSortable(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldNotSortable,
            message: $"Field '{field}' cannot be sorted.");

        /// <summary>The field exists but is marked as not selectable in the map.</summary>
        public static Error NotSelectable(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldNotSelectable,
            message: $"Field '{field}' cannot be selected.");

        /// <summary>The field exists but is marked as not groupable in the map.</summary>
        public static Error NotGroupable(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldNotGroupable,
            message: $"Field '{field}' cannot be grouped.");

        /// <summary>The same field appears more than once in a select, group-by, or order clause.</summary>
        public static Error Duplicate(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldDuplicate,
            message: $"Field '{field}' is specified more than once.");

        // ---- Operator ----

        /// <summary>The operator (e.g., Contains) is used on a field type that does not support it (e.g., an integer).</summary>
        public static Error TypeMismatch(string field, string operatorName) => Error.BadRequest(
            code: QueryErrorCodes.OperatorTypeMismatch,
            message: $"Operator '{operatorName}' is not valid for field '{field}'.");

        /// <summary>The operator is not supported by the current database provider.</summary>
        public static Error OperatorUnsupported(string name) => Error.BadRequest(
            code: QueryErrorCodes.OperatorUnsupported,
            message: $"Operator '{name}' is not supported by this provider.");

        // ---- Order ----

        /// <summary>The ordering text has invalid syntax (unknown keyword, malformed direction, etc.).</summary>
        public static Error OrderSyntax(string detail) => Error.BadRequest(
            code: QueryErrorCodes.OrderSyntax,
            message: $"Invalid ordering: {detail}.");

        // ---- Page ----

        /// <summary>The page number is zero or negative.</summary>
        public static Error PageInvalid => Error.BadRequest(
            code: QueryErrorCodes.PageInvalid,
            message: "Page must be greater than zero.");

        /// <summary>The page size is out of the allowed range (less than 1 or greater than the maximum).</summary>
        public static Error PageSizeInvalid(int max) => Error.BadRequest(
            code: QueryErrorCodes.PageSizeInvalid,
            message: $"Page size must be between 1 and {max}.");

        // ---- Search ----

        /// <summary>A search was requested but no field in the map is marked as searchable.</summary>
        public static Error SearchNotConfigured => Error.BadRequest(
            code: QueryErrorCodes.SearchNotConfigured,
            message: "No searchable field is mapped.");

        /// <summary>The search text exceeds the maximum allowed length.</summary>
        public static Error SearchTooLong(int max) => Error.BadRequest(
            code: QueryErrorCodes.SearchTooLong,
            message: $"Search cannot exceed {max} characters.");

        /// <summary>The search text contains more individual terms than the limit allows.</summary>
        public static Error TooManySearchTerms(int max) => Error.BadRequest(
            code: QueryErrorCodes.SearchTooManyTerms,
            message: $"Search cannot contain more than {max} terms.");

        /// <summary>The search targets more fields than the limit allows.</summary>
        public static Error TooManySearchFields(int max) => Error.BadRequest(
            code: QueryErrorCodes.SearchTooManyFields,
            message: $"Search cannot target more than {max} fields.");

        // ---- Tabular ----

        /// <summary>Select and aggregate are used together without a group-by, which is invalid in SQL.</summary>
        public static Error MixedShape => Error.BadRequest(
            code: QueryErrorCodes.TabularMixedShape,
            message: "Select and aggregate cannot be combined without a group by.");

        /// <summary>A select field is not included in the group-by keys, which is invalid for grouped queries.</summary>
        public static Error FieldNotGrouped(string field) => Error.BadRequest(
            code: QueryErrorCodes.TabularFieldNotGrouped,
            message: $"Selected field '{field}' is not a grouping key.");

        // ---- Aggregate ----

        /// <summary>The aggregate expression has invalid syntax (unknown function, missing parentheses, etc.).</summary>
        public static Error AggregateSyntax(string detail) => Error.BadRequest(
            code: QueryErrorCodes.AggregateSyntax,
            message: $"Invalid aggregate: {detail}.");

        /// <summary>The aggregate function is not supported for the field's type (e.g., Sum on a string).</summary>
        public static Error AggregateUnsupported(string field) => Error.BadRequest(
            code: QueryErrorCodes.AggregateUnsupported,
            message: $"Aggregate is not supported for field '{field}'.");

        // ---- Unsupported ----

        /// <summary>The requested feature is not supported by the current database provider.</summary>
        public static Error Unsupported(string feature) => Error.BadRequest(
            code: QueryErrorCodes.Unsupported,
            message: $"The selected provider does not support {feature}.");

        // ---- Builder ----

        /// <summary>The expression cannot be translated to a query.</summary>
        public static Error ExpressionUnsupported(string detail) => Error.BadRequest(
            code: QueryErrorCodes.BuilderExpressionUnsupported,
            message: $"The expression cannot be translated to a query: {detail}.");

        /// <summary>More than one mapped field matches the requested path.</summary>
        public static Error AmbiguousField(string path) => Error.BadRequest(
            code: QueryErrorCodes.BuilderAmbiguousField,
            message: $"More than one mapped field matches '{path}'.");

        /// <summary>A projected query was requested without a projection expression.</summary>
        public static Error ProjectionRequired => Error.BadRequest(
            code: QueryErrorCodes.BuilderProjectionRequired,
            message: "A projection expression is required for a projected query.");

        /// <summary>The stored projection does not match the requested response type.</summary>
        public static Error ProjectionMismatch => Error.BadRequest(
            code: QueryErrorCodes.BuilderProjectionMismatch,
            message: "The stored projection does not match the requested response type.");

        /// <summary>The query must return entities but does not.</summary>
        public static Error EntityShapeRequired => Error.BadRequest(
            code: QueryErrorCodes.BuilderEntityShapeRequired,
            message: "The query must return entities.");

        /// <summary>The query must return tabular data but does not.</summary>
        public static Error TabularShapeRequired => Error.BadRequest(
            code: QueryErrorCodes.BuilderTabularShapeRequired,
            message: "The query must return tabular data.");

        /// <summary>The requested build conflicts with the query's stored projection or shape.</summary>
        public static Error ShapeConflict(string detail) => Error.BadRequest(
            code: QueryErrorCodes.BuilderShapeConflict,
            message: $"The query shape conflicts with the requested build: {detail}.");

        /// <summary>The requested max-row cap is not a positive number.</summary>
        public static Error MaxRowsInvalid => Error.BadRequest(
            code: QueryErrorCodes.BuilderMaxRowsInvalid,
            message: "Max rows must be greater than zero.");

        /// <summary>The requested eager-loading path is null, empty, or whitespace.</summary>
        public static Error IncludeInvalid => Error.BadRequest(
            code: QueryErrorCodes.BuilderIncludeInvalid,
            message: "An include path is required.");
    }
}
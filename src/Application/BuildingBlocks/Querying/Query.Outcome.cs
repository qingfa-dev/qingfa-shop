namespace Application.BuildingBlocks.Querying;

/// <summary>Factory methods that create typed errors for query parsing and validation failures.</summary>
public static class QueryOutcome
{
    /// <summary>Stable error codes for query parsing, validation, mapping, and execution.</summary>
    public static class Codes
    {
        // ---- Filter ----

        /// <summary>The filter text contains a syntax error (unmatched parenthesis, unexpected token, etc.).</summary>
        public const string FilterSyntax = "Query.Filter.Syntax";

        /// <summary>The filter value cannot be converted to the field's expected type.</summary>
        public const string FilterValueInvalid = "Query.Filter.ValueInvalid";

        /// <summary>The filter text exceeds the maximum allowed length.</summary>
        public const string FilterTooLong = "Query.Filter.TooLong";

        /// <summary>The filter contains more nested parenthesis groups than allowed.</summary>
        public const string FilterDepthExceeded = "Query.Filter.DepthExceeded";

        /// <summary>The filter contains more individual conditions than the limit allows.</summary>
        public const string FilterTooManyConditions = "Query.Filter.TooManyConditions";

        // ---- Field ----

        /// <summary>The referenced field name does not exist in the query map.</summary>
        public const string FieldNotMapped = "Query.Field.NotMapped";

        /// <summary>The field exists but is marked as not filterable in the map.</summary>
        public const string FieldNotFilterable = "Query.Field.NotFilterable";

        /// <summary>The field exists but is marked as not sortable in the map.</summary>
        public const string FieldNotSortable = "Query.Field.NotSortable";

        /// <summary>The field exists but is marked as not selectable in the map.</summary>
        public const string FieldNotSelectable = "Query.Field.NotSelectable";

        /// <summary>The field exists but is marked as not groupable in the map.</summary>
        public const string FieldNotGroupable = "Query.Field.NotGroupable";

        /// <summary>The same field appears more than once in a select, group-by, or order clause.</summary>
        public const string FieldDuplicate = "Query.Field.Duplicate";

        // ---- Operator ----

        /// <summary>The operator is used on a field type that does not support it.</summary>
        public const string OperatorTypeMismatch = "Query.Operator.TypeMismatch";

        /// <summary>The operator is not supported by the current database provider.</summary>
        public const string OperatorUnsupported = "Query.Operator.Unsupported";

        // ---- Order ----

        /// <summary>The ordering text has invalid syntax (unknown keyword, malformed direction, etc.).</summary>
        public const string OrderSyntax = "Query.Order.Syntax";

        // ---- Page ----

        /// <summary>The page number is zero or negative.</summary>
        public const string PageInvalid = "Query.Page.Invalid";

        /// <summary>The page size is out of the allowed range.</summary>
        public const string PageSizeInvalid = "Query.PageSize.Invalid";

        // ---- Search ----

        /// <summary>A search was requested but no field in the map is marked as searchable.</summary>
        public const string SearchNotConfigured = "Query.Search.NotConfigured";

        /// <summary>The search text is missing or whitespace.</summary>
        public const string SearchRequired = "Query.Search.Required";

        /// <summary>The requested search mode is not supported by the request type.</summary>
        public const string SearchModeMismatch = "Query.Search.ModeMismatch";

        /// <summary>The search text exceeds the maximum allowed length.</summary>
        public const string SearchTooLong = "Query.Search.TooLong";

        /// <summary>The search text contains more individual terms than the limit allows.</summary>
        public const string SearchTooManyTerms = "Query.Search.TooManyTerms";

        /// <summary>The search targets more fields than the limit allows.</summary>
        public const string SearchTooManyFields = "Query.Search.TooManyFields";

        // ---- Tabular ----

        /// <summary>Select and aggregate are used together without a group-by.</summary>
        public const string TabularMixedShape = "Query.Tabular.MixedShape";

        /// <summary>A select field is not included in the group-by keys.</summary>
        public const string TabularFieldNotGrouped = "Query.Tabular.FieldNotGrouped";

        // ---- Aggregate ----

        /// <summary>The aggregate expression has invalid syntax (unknown function, missing parentheses, etc.).</summary>
        public const string AggregateSyntax = "Query.Aggregate.Syntax";

        /// <summary>The aggregate function is not supported for the field's type.</summary>
        public const string AggregateUnsupported = "Query.Aggregate.Unsupported";

        // ---- Unsupported ----

        /// <summary>The requested feature is not supported by the current database provider.</summary>
        public const string Unsupported = "Query.Unsupported";

        // ---- Map Declaration ----

        /// <summary>A mapped field is missing a name.</summary>
        public const string MapNameRequired = "Query.Map.NameRequired";

        /// <summary>A mapped field name exceeds the maximum allowed length.</summary>
        public const string MapNameTooLong = "Query.Map.NameTooLong";

        /// <summary>Two or more fields in the same map share the same name.</summary>
        public const string MapDuplicate = "Query.Map.Duplicate";

        /// <summary>A field marked as searchable must be a string type.</summary>
        public const string MapSearchableNotString = "Query.Map.SearchableNotString";

        /// <summary>A composite field must contain at least one sub-field.</summary>
        public const string MapCompositeEmpty = "Query.Map.CompositeEmpty";

        /// <summary>A composite field contains sub-fields with mismatched types.</summary>
        public const string MapCompositeTypeMismatch = "Query.Map.CompositeTypeMismatch";

        // ---- Map Registry ----

        /// <summary>A query map for the same entity type was already registered.</summary>
        public const string MapAlreadyRegistered = "Query.Map.AlreadyRegistered";

        /// <summary>No query map is registered for the requested entity type.</summary>
        public const string MapNotRegistered = "Query.Map.NotRegistered";

        // ---- Builder ----

        /// <summary>The expression cannot be translated to a query.</summary>
        public const string BuilderExpressionUnsupported = "Query.Builder.ExpressionUnsupported";

        /// <summary>More than one mapped field matches the requested path.</summary>
        public const string BuilderAmbiguousField = "Query.Builder.AmbiguousField";

        /// <summary>A projected query was requested without a projection expression.</summary>
        public const string BuilderProjectionRequired = "Query.Builder.ProjectionRequired";

        /// <summary>The stored projection does not match the requested response type.</summary>
        public const string BuilderProjectionMismatch = "Query.Builder.ProjectionMismatch";

        /// <summary>The query must return entities but does not.</summary>
        public const string BuilderEntityShapeRequired = "Query.Builder.EntityShapeRequired";

        /// <summary>The query must return tabular data but does not.</summary>
        public const string BuilderTabularShapeRequired = "Query.Builder.TabularShapeRequired";

        /// <summary>The requested build conflicts with the query's stored projection or shape.</summary>
        public const string BuilderShapeConflict = "Query.Builder.ShapeConflict";

        /// <summary>The requested max-row cap is not a positive number.</summary>
        public const string BuilderMaxRowsInvalid = "Query.Builder.MaxRowsInvalid";

        /// <summary>The requested eager-loading path is null, empty, or whitespace.</summary>
        public const string BuilderIncludeInvalid = "Query.Builder.IncludeInvalid";
    }

    /// <summary>Factory methods for every query error scenario.</summary>
    public static class Failure
    {
        // ---- Filter ----

        /// <summary>The filter text contains a syntax error (unmatched parenthesis, unexpected token, etc.).</summary>
        public static Error Syntax(string detail) => Error.BadRequest(
            code: Codes.FilterSyntax,
            message: $"Invalid filter syntax: {detail}.");

        /// <summary>The filter value cannot be converted to the field's expected type.</summary>
        public static Error ValueInvalid(string field, string value) => Error.BadRequest(
            code: Codes.FilterValueInvalid,
            message: $"Value '{value}' is not valid for field '{field}'.");

        /// <summary>The filter text exceeds the maximum allowed length.</summary>
        public static Error TooLong(int max) => Error.BadRequest(
            code: Codes.FilterTooLong,
            message: $"Filter cannot exceed {max} characters.");

        /// <summary>The filter contains more nested parenthesis groups than allowed.</summary>
        public static Error DepthExceeded(int max) => Error.BadRequest(
            code: Codes.FilterDepthExceeded,
            message: $"Filter nesting cannot exceed {max} levels.");

        /// <summary>The filter contains more individual conditions than the limit allows.</summary>
        public static Error TooManyConditions(int max) => Error.BadRequest(
            code: Codes.FilterTooManyConditions,
            message: $"Filter cannot contain more than {max} conditions.");

        // ---- Field ----

        /// <summary>The referenced field name does not exist in the query map.</summary>
        public static Error NotMapped(string field) => Error.BadRequest(
            code: Codes.FieldNotMapped,
            message: $"Field '{field}' is not mapped.");

        /// <summary>The field exists but is marked as not filterable in the map.</summary>
        public static Error NotFilterable(string field) => Error.BadRequest(
            code: Codes.FieldNotFilterable,
            message: $"Field '{field}' cannot be filtered.");

        /// <summary>The field exists but is marked as not sortable in the map.</summary>
        public static Error NotSortable(string field) => Error.BadRequest(
            code: Codes.FieldNotSortable,
            message: $"Field '{field}' cannot be sorted.");

        /// <summary>The field exists but is marked as not selectable in the map.</summary>
        public static Error NotSelectable(string field) => Error.BadRequest(
            code: Codes.FieldNotSelectable,
            message: $"Field '{field}' cannot be selected.");

        /// <summary>The field exists but is marked as not groupable in the map.</summary>
        public static Error NotGroupable(string field) => Error.BadRequest(
            code: Codes.FieldNotGroupable,
            message: $"Field '{field}' cannot be grouped.");

        /// <summary>The same field appears more than once in a select, group-by, or order clause.</summary>
        public static Error Duplicate(string field) => Error.BadRequest(
            code: Codes.FieldDuplicate,
            message: $"Field '{field}' is specified more than once.");

        // ---- Operator ----

        /// <summary>The operator (e.g., Contains) is used on a field type that does not support it (e.g., an integer).</summary>
        public static Error TypeMismatch(string field, string operatorName) => Error.BadRequest(
            code: Codes.OperatorTypeMismatch,
            message: $"Operator '{operatorName}' is not valid for field '{field}'.");

        /// <summary>The operator is not supported by the current database provider.</summary>
        public static Error OperatorUnsupported(string name) => Error.BadRequest(
            code: Codes.OperatorUnsupported,
            message: $"Operator '{name}' is not supported by this provider.");

        // ---- Order ----

        /// <summary>The ordering text has invalid syntax (unknown keyword, malformed direction, etc.).</summary>
        public static Error OrderSyntax(string detail) => Error.BadRequest(
            code: Codes.OrderSyntax,
            message: $"Invalid ordering: {detail}.");

        // ---- Page ----

        /// <summary>The page number is zero or negative.</summary>
        public static Error PageInvalid => Error.BadRequest(
            code: Codes.PageInvalid,
            message: "Page must be greater than zero.");

        /// <summary>The page size is out of the allowed range (less than 1 or greater than the maximum).</summary>
        public static Error PageSizeInvalid(int max) => Error.BadRequest(
            code: Codes.PageSizeInvalid,
            message: $"Page size must be between 1 and {max}.");

        // ---- Search ----

        /// <summary>A search was requested but no field in the map is marked as searchable.</summary>
        public static Error SearchNotConfigured => Error.BadRequest(
            code: Codes.SearchNotConfigured,
            message: "No searchable field is mapped.");

        /// <summary>The search text is missing or whitespace.</summary>
        public static Error SearchRequired => Error.BadRequest(
            code: Codes.SearchRequired,
            message: "Search text is required.");

        /// <summary>The requested search mode is not supported by the request type.</summary>
        public static Error SearchModeMismatch(string mode) => Error.BadRequest(
            code: Codes.SearchModeMismatch,
            message: $"Search mode '{mode}' is not valid for this request.");

        /// <summary>The search text exceeds the maximum allowed length.</summary>
        public static Error SearchTooLong(int max) => Error.BadRequest(
            code: Codes.SearchTooLong,
            message: $"Search cannot exceed {max} characters.");

        /// <summary>The search text contains more individual terms than the limit allows.</summary>
        public static Error TooManySearchTerms(int max) => Error.BadRequest(
            code: Codes.SearchTooManyTerms,
            message: $"Search cannot contain more than {max} terms.");

        /// <summary>The search targets more fields than the limit allows.</summary>
        public static Error TooManySearchFields(int max) => Error.BadRequest(
            code: Codes.SearchTooManyFields,
            message: $"Search cannot target more than {max} fields.");

        // ---- Tabular ----

        /// <summary>Select and aggregate are used together without a group-by, which is invalid in SQL.</summary>
        public static Error MixedShape => Error.BadRequest(
            code: Codes.TabularMixedShape,
            message: "Select and aggregate cannot be combined without a group by.");

        /// <summary>A select field is not included in the group-by keys, which is invalid for grouped queries.</summary>
        public static Error FieldNotGrouped(string field) => Error.BadRequest(
            code: Codes.TabularFieldNotGrouped,
            message: $"Selected field '{field}' is not a grouping key.");

        // ---- Aggregate ----

        /// <summary>The aggregate expression has invalid syntax (unknown function, missing parentheses, etc.).</summary>
        public static Error AggregateSyntax(string detail) => Error.BadRequest(
            code: Codes.AggregateSyntax,
            message: $"Invalid aggregate: {detail}.");

        /// <summary>The aggregate function is not supported for the field's type (e.g., Sum on a string).</summary>
        public static Error AggregateUnsupported(string field) => Error.BadRequest(
            code: Codes.AggregateUnsupported,
            message: $"Aggregate is not supported for field '{field}'.");

        // ---- Unsupported ----

        /// <summary>The requested feature is not supported by the current database provider.</summary>
        public static Error Unsupported(string feature) => Error.BadRequest(
            code: Codes.Unsupported,
            message: $"The selected provider does not support {feature}.");

        // ---- Builder ----

        /// <summary>The expression cannot be translated to a query.</summary>
        public static Error ExpressionUnsupported(string detail) => Error.BadRequest(
            code: Codes.BuilderExpressionUnsupported,
            message: $"The expression cannot be translated to a query: {detail}.");

        /// <summary>More than one mapped field matches the requested path.</summary>
        public static Error AmbiguousField(string path) => Error.BadRequest(
            code: Codes.BuilderAmbiguousField,
            message: $"More than one mapped field matches '{path}'.");

        /// <summary>A projected query was requested without a projection expression.</summary>
        public static Error ProjectionRequired => Error.BadRequest(
            code: Codes.BuilderProjectionRequired,
            message: "A projection expression is required for a projected query.");

        /// <summary>The stored projection does not match the requested response type.</summary>
        public static Error ProjectionMismatch => Error.BadRequest(
            code: Codes.BuilderProjectionMismatch,
            message: "The stored projection does not match the requested response type.");

        /// <summary>The query must return entities but does not.</summary>
        public static Error EntityShapeRequired => Error.BadRequest(
            code: Codes.BuilderEntityShapeRequired,
            message: "The query must return entities.");

        /// <summary>The query must return tabular data but does not.</summary>
        public static Error TabularShapeRequired => Error.BadRequest(
            code: Codes.BuilderTabularShapeRequired,
            message: "The query must return tabular data.");

        /// <summary>The requested build conflicts with the query's stored projection or shape.</summary>
        public static Error ShapeConflict(string detail) => Error.BadRequest(
            code: Codes.BuilderShapeConflict,
            message: $"The query shape conflicts with the requested build: {detail}.");

        /// <summary>The requested max-row cap is not a positive number.</summary>
        public static Error MaxRowsInvalid => Error.BadRequest(
            code: Codes.BuilderMaxRowsInvalid,
            message: "Max rows must be greater than zero.");

        /// <summary>The requested eager-loading path is null, empty, or whitespace.</summary>
        public static Error IncludeInvalid => Error.BadRequest(
            code: Codes.BuilderIncludeInvalid,
            message: "An include path is required.");
    }
}
namespace Application.Common.Querying;

/// <summary>Stable error codes for query parsing, validation, mapping, and execution.</summary>
/// <remarks>
/// Centralizes all error codes so they can be referenced in tests, logging, and contracts
/// without duplicating string literals.
/// </remarks>
public static class QueryErrorCodes
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
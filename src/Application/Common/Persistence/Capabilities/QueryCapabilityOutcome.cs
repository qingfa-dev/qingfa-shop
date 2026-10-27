namespace Application.Common.Persistence.Capabilities;

/// <summary>Query capability validation failures.</summary>
public static class QueryCapabilityOutcome
{
    public static class Failure
    {
        // ---- Filter ----
        public static Error FilterTooLong(string parameterName = "filter") => Error.Validation(
            Codes.FilterTooLong,
            $"{parameterName} cannot exceed {QueryCapabilityConstant.Filter.MaxLength} characters.");

        public static Error FilterTooDeep(string parameterName = "filter") => Error.Validation(
            Codes.FilterTooDeep,
            $"{parameterName} nesting depth cannot exceed {QueryCapabilityConstant.Filter.MaxDepth}.");

        public static Error FilterTooManyConditions(string parameterName = "filter") => Error.Validation(
            Codes.FilterTooManyConditions,
            $"{parameterName} cannot contain more than {QueryCapabilityConstant.Filter.MaxConditions} conditions.");

        public static Error FilterInvalidOperator(string parameterName = "filter", string? operatorName = null) => Error.Validation(
            Codes.FilterInvalidOperator,
            $"{parameterName} contains an invalid operator.{(!string.IsNullOrEmpty(operatorName) ? $" Operator: {operatorName}" : "")}");

        public static Error FilterInvalidSyntax(string parameterName = "filter", string? reason = null) => Error.Validation(
            Codes.FilterInvalidSyntax,
            $"{parameterName} has invalid syntax.{(!string.IsNullOrEmpty(reason) ? $" Reason: {reason}" : "")}");

        // ---- Order ----
        public static Error OrderTooManyFields(string parameterName = "orderBy") => Error.Validation(
            Codes.OrderTooManyFields,
            $"{parameterName} cannot contain more than {QueryCapabilityConstant.Order.MaxFields} fields.");

        public static Error OrderInvalidField(string parameterName = "orderBy", string? fieldName = null) => Error.Validation(
            Codes.OrderInvalidField,
            $"{parameterName} contains an invalid field.{(!string.IsNullOrEmpty(fieldName) ? $" Field: {fieldName}" : "")}");

        public static Error OrderInvalidDirection(string parameterName = "orderBy", string? direction = null) => Error.Validation(
            Codes.OrderInvalidDirection,
            $"{parameterName} contains an invalid direction.{(!string.IsNullOrEmpty(direction) ? $" Direction: {direction}" : "")}");

        // ---- Select / Projection ----
        public static Error SelectTooManyColumns(string parameterName = "select") => Error.Validation(
            Codes.SelectTooManyColumns,
            $"{parameterName} cannot contain more than {QueryCapabilityConstant.Select.MaxColumns} columns.");

        public static Error SelectInvalidColumn(string parameterName = "select", string? columnName = null) => Error.Validation(
            Codes.SelectInvalidColumn,
            $"{parameterName} contains an invalid column.{(!string.IsNullOrEmpty(columnName) ? $" Column: {columnName}" : "")}");

        // ---- Group ----
        public static Error GroupTooManyKeys(string parameterName = "groupBy") => Error.Validation(
            Codes.GroupTooManyKeys,
            $"{parameterName} cannot contain more than {QueryCapabilityConstant.Group.MaxKeys} keys.");

        public static Error GroupInvalidKey(string parameterName = "groupBy", string? keyName = null) => Error.Validation(
            Codes.GroupInvalidKey,
            $"{parameterName} contains an invalid key.{(!string.IsNullOrEmpty(keyName) ? $" Key: {keyName}" : "")}");

        // ---- Aggregate ----
        public static Error AggregateTooMany(string parameterName = "aggregate") => Error.Validation(
            Codes.AggregateTooMany,
            $"{parameterName} cannot contain more than {QueryCapabilityConstant.Aggregate.MaxCount} aggregates.");

        public static Error AggregateInvalidFunction(string parameterName = "aggregate", string? functionName = null) => Error.Validation(
            Codes.AggregateInvalidFunction,
            $"{parameterName} contains an invalid function.{(!string.IsNullOrEmpty(functionName) ? $" Function: {functionName}" : "")}");

        public static Error AggregateInvalidExpression(string parameterName = "aggregate", string? expression = null) => Error.Validation(
            Codes.AggregateInvalidExpression,
            $"{parameterName} contains an invalid expression.{(!string.IsNullOrEmpty(expression) ? $" Expression: {expression}" : "")}");

        // ---- Search ----
        public static Error SearchTooLong(string parameterName = "search") => Error.Validation(
            Codes.SearchTooLong,
            $"{parameterName} cannot exceed {QueryCapabilityConstant.Search.MaxLength} characters.");

        public static Error SearchTooManyTerms(string parameterName = "search") => Error.Validation(
            Codes.SearchTooManyTerms,
            $"{parameterName} cannot contain more than {QueryCapabilityConstant.Search.MaxTerms} terms.");

        public static Error SearchTooManyFields(string parameterName = "search") => Error.Validation(
            Codes.SearchTooManyFields,
            $"{parameterName} cannot target more than {QueryCapabilityConstant.Search.MaxFields} fields.");

        public static Error SearchInvalidMode(string parameterName = "search", string? mode = null) => Error.Validation(
            Codes.SearchInvalidMode,
            $"{parameterName} has an invalid mode.{(!string.IsNullOrEmpty(mode) ? $" Mode: {mode}" : "")}");

        // ---- Map ----
        public static Error MapFieldNameTooLong(string parameterName = "fieldName") => Error.Validation(
            Codes.MapFieldNameTooLong,
            $"{parameterName} cannot exceed {QueryCapabilityConstant.Map.MaxFieldNameLength} characters.");

        // ---- Capability Support ----
        public static Error CapabilityNotSupported(string capability, string? providerName = null) => Error.Validation(
            Codes.CapabilityNotSupported,
            $"Capability '{capability}' is not supported.{(!string.IsNullOrEmpty(providerName) ? $" Provider: {providerName}" : "")}");

        public static Error SearchModeNotSupported(string mode, string? providerName = null) => Error.Validation(
            Codes.SearchModeNotSupported,
            $"Search mode '{mode}' is not supported.{(!string.IsNullOrEmpty(providerName) ? $" Provider: {providerName}" : "")}");

        public static Error PageSizeExceedsProviderLimit(int requested, int max, string parameterName = "pageSize") => Error.Validation(
            Codes.PageSizeExceedsProviderLimit,
            $"Requested {parameterName} {requested} exceeds the provider maximum of {max}.");

        public static Error ExpressionDepthExceedsProviderLimit(int depth, int max, string parameterName = "expression") => Error.Validation(
            Codes.ExpressionDepthExceedsProviderLimit,
            $"Expression {parameterName} depth {depth} exceeds the provider maximum of {max}.");

        public static Error OrderByClausesExceedProviderLimit(int count, int max, string parameterName = "orderBy") => Error.Validation(
            Codes.OrderByClausesExceedProviderLimit,
            $"{parameterName} clause count {count} exceeds the provider maximum of {max}.");
    }

    /// <summary>Stable codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        // ---- Filter ----
        public const string FilterTooLong           = "QueryCapability.Filter.TooLong";
        public const string FilterTooDeep           = "QueryCapability.Filter.TooDeep";
        public const string FilterTooManyConditions = "QueryCapability.Filter.TooManyConditions";
        public const string FilterInvalidOperator   = "QueryCapability.Filter.InvalidOperator";
        public const string FilterInvalidSyntax     = "QueryCapability.Filter.InvalidSyntax";

        // ---- Order ----
        public const string OrderTooManyFields  = "QueryCapability.Order.TooManyFields";
        public const string OrderInvalidField   = "QueryCapability.Order.InvalidField";
        public const string OrderInvalidDirection = "QueryCapability.Order.InvalidDirection";

        // ---- Select / Projection ----
        public const string SelectTooManyColumns = "QueryCapability.Select.TooManyColumns";
        public const string SelectInvalidColumn  = "QueryCapability.Select.InvalidColumn";

        // ---- Group ----
        public const string GroupTooManyKeys = "QueryCapability.Group.TooManyKeys";
        public const string GroupInvalidKey  = "QueryCapability.Group.InvalidKey";

        // ---- Aggregate ----
        public const string AggregateTooMany       = "QueryCapability.Aggregate.TooMany";
        public const string AggregateInvalidFunction = "QueryCapability.Aggregate.InvalidFunction";
        public const string AggregateInvalidExpression = "QueryCapability.Aggregate.InvalidExpression";

        // ---- Search ----
        public const string SearchTooLong          = "QueryCapability.Search.TooLong";
        public const string SearchTooManyTerms     = "QueryCapability.Search.TooManyTerms";
        public const string SearchTooManyFields    = "QueryCapability.Search.TooManyFields";
        public const string SearchInvalidMode      = "QueryCapability.Search.InvalidMode";

        // ---- Map ----
        public const string MapFieldNameTooLong = "QueryCapability.Map.FieldNameTooLong";

        // ---- Capability Support ----
        public const string CapabilityNotSupported            = "QueryCapability.Capability.NotSupported";
        public const string SearchModeNotSupported            = "QueryCapability.SearchMode.NotSupported";
        public const string PageSizeExceedsProviderLimit      = "QueryCapability.PageSize.ExceedsProviderLimit";
        public const string ExpressionDepthExceedsProviderLimit = "QueryCapability.ExpressionDepth.ExceedsProviderLimit";
        public const string OrderByClausesExceedProviderLimit   = "QueryCapability.OrderBy.ExceedsProviderLimit";
    }
}

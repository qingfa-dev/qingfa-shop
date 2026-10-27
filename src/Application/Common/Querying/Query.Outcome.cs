namespace Application.Common.Querying;
/// <summary>Error codes for query parsing and validation.</summary>
public static class QueryOutcome
{
    public static class Failure
    {
        // ---- Filter ----
        public static Error Syntax(string detail) => Error.BadRequest(
            code: QueryErrorCodes.FilterSyntax,
            message: $"Invalid filter syntax: {detail}.");

        public static Error ValueInvalid(string field, string value) => Error.BadRequest(
            code: QueryErrorCodes.FilterValueInvalid,
            message: $"Value '{value}' is not valid for field '{field}'.");

        public static Error TooLong(int max) => Error.BadRequest(
            code: QueryErrorCodes.FilterTooLong,
            message: $"Filter cannot exceed {max} characters.");

        public static Error DepthExceeded(int max) => Error.BadRequest(
            code: QueryErrorCodes.FilterDepthExceeded,
            message: $"Filter nesting cannot exceed {max} levels.");

        public static Error TooManyConditions(int max) => Error.BadRequest(
            code: QueryErrorCodes.FilterTooManyConditions,
            message: $"Filter cannot contain more than {max} conditions.");

        // ---- Field ----
        public static Error NotMapped(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldNotMapped,
            message: $"Field '{field}' is not mapped.");

        public static Error NotFilterable(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldNotFilterable,
            message: $"Field '{field}' cannot be filtered.");

        public static Error NotSortable(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldNotSortable,
            message: $"Field '{field}' cannot be sorted.");

        public static Error NotSelectable(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldNotSelectable,
            message: $"Field '{field}' cannot be selected.");

        public static Error NotGroupable(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldNotGroupable,
            message: $"Field '{field}' cannot be grouped.");

        public static Error Duplicate(string field) => Error.BadRequest(
            code: QueryErrorCodes.FieldDuplicate,
            message: $"Field '{field}' is specified more than once.");

        // ---- Operator ----
        public static Error TypeMismatch(string field, string operatorName) => Error.BadRequest(
            code: QueryErrorCodes.OperatorTypeMismatch,
            message: $"Operator '{operatorName}' is not valid for field '{field}'.");

        public static Error OperatorUnsupported(string name) => Error.BadRequest(
            code: QueryErrorCodes.OperatorUnsupported,
            message: $"Operator '{name}' is not supported by this provider.");

        // ---- Order ----
        public static Error OrderSyntax(string detail) => Error.BadRequest(
            code: QueryErrorCodes.OrderSyntax,
            message: $"Invalid ordering: {detail}.");

        // ---- Page ----
        public static Error PageInvalid => Error.BadRequest(
            code: QueryErrorCodes.PageInvalid,
            message: "Page must be greater than zero.");

        public static Error PageSizeInvalid(int max) => Error.BadRequest(
            code: QueryErrorCodes.PageSizeInvalid,
            message: $"Page size must be between 1 and {max}.");

        // ---- Search ----
        public static Error SearchNotConfigured => Error.BadRequest(
            code: QueryErrorCodes.SearchNotConfigured,
            message: "No searchable field is mapped.");

        public static Error SearchTooLong(int max) => Error.BadRequest(
            code: QueryErrorCodes.SearchTooLong,
            message: $"Search cannot exceed {max} characters.");

        public static Error TooManySearchTerms(int max) => Error.BadRequest(
            code: QueryErrorCodes.SearchTooManyTerms,
            message: $"Search cannot contain more than {max} terms.");

        public static Error TooManySearchFields(int max) => Error.BadRequest(
            code: QueryErrorCodes.SearchTooManyFields,
            message: $"Search cannot target more than {max} fields.");

        public static Error SearchLanguageTooLong(int max) => Error.BadRequest(
            code: QueryErrorCodes.SearchLanguageTooLong,
            message: $"Search language cannot exceed {max} characters.");

        public static Error InvalidMinScore => Error.BadRequest(
            code: QueryErrorCodes.SearchInvalidMinScore,
            message: "Minimum score must be between 0 and 1.");

        // ---- Tabular ----
        public static Error MixedShape => Error.BadRequest(
            code: QueryErrorCodes.TabularMixedShape,
            message: "Select and aggregate cannot be combined without a group by.");

        public static Error FieldNotGrouped(string field) => Error.BadRequest(
            code: QueryErrorCodes.TabularFieldNotGrouped,
            message: $"Selected field '{field}' is not a grouping key.");

        // ---- Aggregate ----
        public static Error AggregateSyntax(string detail) => Error.BadRequest(
            code: QueryErrorCodes.AggregateSyntax,
            message: $"Invalid aggregate: {detail}.");

        public static Error AggregateUnsupported(string field) => Error.BadRequest(
            code: QueryErrorCodes.AggregateUnsupported,
            message: $"Aggregate is not supported for field '{field}'.");

        // ---- Unsupported ----
        public static Error Unsupported(string feature) => Error.BadRequest(
            code: QueryErrorCodes.Unsupported,
            message: $"The selected provider does not support {feature}.");
    }
}